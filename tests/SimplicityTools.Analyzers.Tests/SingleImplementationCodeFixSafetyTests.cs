using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using SimplicityTools.Analyzers.CodeFixes;
using Xunit;
using static SimplicityTools.Analyzers.Tests.AnalyzerTestInfrastructure;

namespace SimplicityTools.Analyzers.Tests;

public sealed class SingleImplementationCodeFixSafetyTests
{
    private static readonly Dictionary<string, string> IncludePublicApiOptions = new(StringComparer.Ordinal)
    {
        ["simplicity_first.include_public_api"] = "true"
    };

    [Fact]
    public async Task Fix_IsNotOffered_WhenNameofReferencesTheInterface()
    {
        var project = CreateProject(
            [
                new SourceFile("/repo/Feature.cs", """
                namespace Demo;

                internal interface IPricer
                {
                    decimal Price();
                }

                internal sealed class DefaultPricer : IPricer
                {
                    public decimal Price() => 12m;
                }
                """),
                new SourceFile("/repo/Logging.cs", """
                namespace Demo;

                internal static class Logging
                {
                    public static string Describe() => nameof(IPricer);
                }
                """)
            ]);

        var diagnostic = await GetSingleImplementationDiagnosticAsync(project, "IPricer");
        var actions = await GetRegisteredCodeActionsAsync(
            project.Documents.Single(static document => document.FilePath == "/repo/Feature.cs"),
            diagnostic);

        Assert.Empty(actions);
    }

    [Fact]
    public async Task Fix_IsNotOffered_WhenImplementationIsLessAccessibleThanInterface()
    {
        var project = CreateProject(
            [
                new SourceFile("/repo/Feature.cs", """
                namespace Demo;

                public interface IPricer
                {
                    decimal Price();
                }

                internal sealed class DefaultPricer : IPricer
                {
                    public decimal Price() => 12m;
                }
                """)
            ]);

        var diagnostic = await GetSingleImplementationDiagnosticAsync(project, "IPricer", IncludePublicApiOptions);
        var actions = await GetRegisteredCodeActionsAsync(
            project.Documents.Single(static document => document.FilePath == "/repo/Feature.cs"),
            diagnostic);

        Assert.Empty(actions);
    }

    [Fact]
    public async Task Fix_MarksDependencyInjectionStyleRegistrationsForReview()
    {
        var project = CreateProject(
            [
                new SourceFile("/repo/Feature.cs", """
                namespace Demo;

                internal interface IPricer
                {
                    decimal Price();
                }

                internal sealed class DefaultPricer : IPricer
                {
                    public decimal Price() => 12m;
                }

                internal static class ServiceCollection
                {
                    public static void AddScoped<TService, TImplementation>() where TImplementation : TService
                    {
                    }
                }

                internal static class Startup
                {
                    public static void Configure()
                    {
                        ServiceCollection.AddScoped<IPricer, DefaultPricer>();
                    }
                }
                """)
            ]);

        var diagnostic = await GetSingleImplementationDiagnosticAsync(project, "IPricer");
        var document = project.Documents.Single(static document => document.FilePath == "/repo/Feature.cs");
        var appliedFix = await ApplyCodeFixAsync(document, diagnostic, new SingleImplementationInterfaceCodeFixProvider());

        var updatedProject = appliedFix.ChangedSolution.GetProject(project.Id)!;
        var updatedSource = (await updatedProject.Documents.Single(static document => document.FilePath == "/repo/Feature.cs").GetTextAsync()).ToString();

        Assert.DoesNotContain("interface IPricer", updatedSource, StringComparison.Ordinal);
        Assert.Contains("AddScoped<DefaultPricer, DefaultPricer>()", updatedSource, StringComparison.Ordinal);
        Assert.Contains("// TODO: review DI registration: interface 'IPricer' was replaced with 'DefaultPricer'.", updatedSource, StringComparison.Ordinal);

        var reviewCommentIndex = updatedSource.IndexOf("// TODO: review DI registration", StringComparison.Ordinal);
        var registrationIndex = updatedSource.IndexOf("AddScoped<DefaultPricer, DefaultPricer>()", StringComparison.Ordinal);
        Assert.True(reviewCommentIndex >= 0 && reviewCommentIndex < registrationIndex, "The review comment should precede the rewritten registration.");

        Assert.Empty(await GetCompilationDiagnosticsAsync(updatedProject));
    }

    [Fact]
    public async Task Fix_HoistsMembersIntoDependentInterfaceInAnotherFile_WithResolvableTypes()
    {
        var project = CreateProject(
            [
                new SourceFile("/repo/Pricing.cs", """
                using System.Collections.Generic;

                namespace Demo;

                internal interface IPricer
                {
                    IReadOnlyList<decimal> Prices();
                }

                internal sealed class DefaultPricer : ICheckoutPricer
                {
                    public System.Collections.Generic.IReadOnlyList<decimal> Prices() => new decimal[] { 12m };

                    public decimal Discount() => 1m;
                }
                """),
                // Deliberately no using directive for System.Collections.Generic: the hoisted
                // member must be rendered so its types resolve in this file regardless.
                new SourceFile("/repo/Checkout.cs", """
                namespace Demo;

                internal interface ICheckoutPricer : IPricer
                {
                    decimal Discount();
                }
                """)
            ]);

        var diagnostic = await GetSingleImplementationDiagnosticAsync(project, "IPricer");
        var document = project.Documents.Single(static document => document.FilePath == "/repo/Pricing.cs");
        var appliedFix = await ApplyCodeFixAsync(document, diagnostic, new SingleImplementationInterfaceCodeFixProvider());

        var updatedProject = appliedFix.ChangedSolution.GetProject(project.Id)!;
        var checkoutSource = (await updatedProject.Documents.Single(static document => document.FilePath == "/repo/Checkout.cs").GetTextAsync()).ToString();
        var pricingSource = (await updatedProject.Documents.Single(static document => document.FilePath == "/repo/Pricing.cs").GetTextAsync()).ToString();

        Assert.DoesNotContain("interface IPricer", pricingSource, StringComparison.Ordinal);
        Assert.DoesNotContain(": IPricer", checkoutSource, StringComparison.Ordinal);
        Assert.Contains("Prices();", checkoutSource, StringComparison.Ordinal);

        Assert.Empty(await GetCompilationDiagnosticsAsync(updatedProject));
    }

    [Fact]
    public async Task Fix_RewritesMetadataResolvedReferencesInOtherProjects()
    {
        using var workspace = new AdhocWorkspace();
        var libraryProjectId = ProjectId.CreateNewId();
        var consumerProjectId = ProjectId.CreateNewId();

        var solution = workspace.CurrentSolution
            .AddProject(libraryProjectId, "PricingLibrary", "PricingLibrary", LanguageNames.CSharp)
            .WithProjectCompilationOptions(libraryProjectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithProjectParseOptions(libraryProjectId, new CSharpParseOptions(LanguageVersion.Preview))
            .AddMetadataReferences(libraryProjectId, GetTrustedPlatformReferences())
            .AddDocument(DocumentId.CreateNewId(libraryProjectId), "Pricing.cs", SourceText.From("""
                namespace Demo;

                public interface IPricer
                {
                    decimal Price();
                }

                public sealed class DefaultPricer : IPricer
                {
                    public decimal Price() => 12m;
                }
                """), filePath: "/library/Pricing.cs");

        // Reference the library as a compiled PE image (not a ProjectReference) so the consumer
        // compilation resolves IPricer/DefaultPricer through metadata symbols. The rewrite must
        // still find those references via compilation-independent identity.
        var libraryCompilation = await solution.GetProject(libraryProjectId)!.GetCompilationAsync()
            ?? throw new InvalidOperationException("Library compilation was not available.");
        using var peStream = new MemoryStream();
        var emitResult = libraryCompilation.Emit(peStream);
        Assert.True(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics));
        var libraryImage = MetadataReference.CreateFromImage(peStream.ToArray());

        solution = solution
            .AddProject(consumerProjectId, "Consumer", "Consumer", LanguageNames.CSharp)
            .WithProjectCompilationOptions(consumerProjectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithProjectParseOptions(consumerProjectId, new CSharpParseOptions(LanguageVersion.Preview))
            .AddMetadataReferences(consumerProjectId, GetTrustedPlatformReferences())
            .AddMetadataReference(consumerProjectId, libraryImage)
            .AddDocument(DocumentId.CreateNewId(consumerProjectId), "Checkout.cs", SourceText.From("""
                namespace Shop;

                using Demo;

                public sealed class Checkout(IPricer pricer)
                {
                    public IPricer Pricer { get; } = pricer;

                    public decimal Total() => Pricer.Price();
                }
                """), filePath: "/consumer/Checkout.cs");

        Assert.True(workspace.TryApplyChanges(solution));

        var libraryProject = workspace.CurrentSolution.GetProject(libraryProjectId)!;
        var diagnostic = await GetSingleImplementationDiagnosticAsync(libraryProject, "IPricer", IncludePublicApiOptions);
        var document = libraryProject.Documents.Single(static document => document.FilePath == "/library/Pricing.cs");
        var appliedFix = await ApplyCodeFixAsync(document, diagnostic, new SingleImplementationInterfaceCodeFixProvider());

        var consumerSource = (await appliedFix.ChangedSolution.GetProject(consumerProjectId)!
            .Documents.Single(static document => document.FilePath == "/consumer/Checkout.cs")
            .GetTextAsync()).ToString();

        Assert.DoesNotContain("IPricer", consumerSource, StringComparison.Ordinal);
        Assert.Contains("Checkout(DefaultPricer pricer)", consumerSource, StringComparison.Ordinal);
        Assert.Contains("public DefaultPricer Pricer { get; }", consumerSource, StringComparison.Ordinal);

        Assert.Empty(await GetCompilationDiagnosticsAsync(appliedFix.ChangedSolution.GetProject(libraryProjectId)!));
        Assert.Empty(await GetCompilationDiagnosticsAsync(appliedFix.ChangedSolution.GetProject(consumerProjectId)!));
    }

    private static async Task<Diagnostic> GetSingleImplementationDiagnosticAsync(
        Project project,
        string interfaceName,
        IReadOnlyDictionary<string, string>? globalOptions = null)
    {
        var diagnostics = await GetAnalyzerDiagnosticsAsync(
            project,
            new SingleImplementationInterfaceAnalyzer(),
            globalOptions: globalOptions);
        return Assert.Single(diagnostics.Where(diagnostic =>
            diagnostic.GetMessage().StartsWith($"Interface {interfaceName} ", StringComparison.Ordinal)));
    }

    private static async Task<IReadOnlyList<CodeAction>> GetRegisteredCodeActionsAsync(Document document, Diagnostic diagnostic)
    {
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
        await new SingleImplementationInterfaceCodeFixProvider().RegisterCodeFixesAsync(context);
        return actions;
    }

    private static ImmutableArray<MetadataReference> GetTrustedPlatformReferences()
    {
        var trustedPlatformAssemblies = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)
            ?.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            ?? throw new InvalidOperationException("Trusted platform assemblies were not available.");

        return [.. trustedPlatformAssemblies.Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))];
    }
}
