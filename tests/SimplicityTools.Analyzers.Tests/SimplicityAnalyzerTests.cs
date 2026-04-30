using SimplicityTools.Analyzers;
using SimplicityTools.Analyzers.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Xunit;
using static SimplicityTools.Analyzers.Tests.AnalyzerTestInfrastructure;

namespace SimplicityTools.Analyzers.Tests;

public sealed class SimplicityAnalyzerTests
{
    [Fact]
    public async Task SingleImplementationInterfaceAnalyzer_ReportsSingleConcreteImplementation()
    {
        var diagnostics = await RunAsync(
            new SingleImplementationInterfaceAnalyzer(),
            [
                new SourceFile("/repo/Feature.cs", """
                namespace Demo;

                public interface IPricer
                {
                    decimal Price();
                }

                public sealed class DefaultPricer : IPricer
                {
                    public decimal Price() => 12m;
                }
                """)
            ]);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(SingleImplementationInterfaceAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("IPricer", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("DefaultPricer", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SingleImplementationInterfaceAnalyzer_DoesNotReportWhenTwoImplementationsExist()
    {
        var diagnostics = await RunAsync(
            new SingleImplementationInterfaceAnalyzer(),
            [
                new SourceFile("/repo/Feature.cs", """
                namespace Demo;

                public interface IPricer
                {
                    decimal Price();
                }

                public sealed class DefaultPricer : IPricer
                {
                    public decimal Price() => 12m;
                }

                public sealed class DiscountPricer : IPricer
                {
                    public decimal Price() => 10m;
                }
                """)
            ]);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task SingleImplementationInterfaceCodeFixProvider_RewritesReferencesAndRemovesInterface()
    {
        var project = CreateProject(
            [
                new SourceFile("/repo/Feature.cs", """
                namespace Demo;

                public interface IPricer
                {
                    decimal Price();
                }

                public sealed class DefaultPricer : IPricer
                {
                    public decimal Price() => 12m;
                }

                public sealed class Checkout(IPricer pricer)
                {
                    public IPricer Pricer { get; } = pricer;

                    public decimal Total() => Pricer.Price();
                }
                """)
            ]);

        var analyzer = new SingleImplementationInterfaceAnalyzer();
        var diagnostics = await GetAnalyzerDiagnosticsAsync(project, analyzer);
        var diagnostic = Assert.Single(diagnostics);
        var codeFix = new SingleImplementationInterfaceCodeFixProvider();
        var document = project.Documents.Single(static document => document.FilePath == "/repo/Feature.cs");

        var appliedFix = await ApplyCodeFixAsync(document, diagnostic, codeFix);
        Assert.Contains(appliedFix.PreviewOperations, static operation => operation is ApplyChangesOperation);

        var updatedProject = appliedFix.ChangedSolution.GetProject(project.Id)!;
        var updatedText = await updatedProject.Documents.Single(static document => document.FilePath == "/repo/Feature.cs").GetTextAsync();
        var updatedSource = updatedText.ToString();

        Assert.DoesNotContain("interface IPricer", updatedSource, StringComparison.Ordinal);
        Assert.DoesNotContain(": IPricer", updatedSource, StringComparison.Ordinal);
        Assert.Contains("Checkout(DefaultPricer pricer)", updatedSource, StringComparison.Ordinal);
        Assert.Contains("public DefaultPricer Pricer { get; }", updatedSource, StringComparison.Ordinal);

        var compilationDiagnostics = await GetCompilationDiagnosticsAsync(updatedProject);
        Assert.Empty(compilationDiagnostics);
    }

    [Fact]
    public async Task SingleImplementationInterfaceCodeFixProvider_PreservesDependentInterfaceChains()
    {
        var project = CreateProject(
            [
                new SourceFile("/repo/Feature.cs", """
                namespace Demo;

                public interface IPricer
                {
                    decimal Price();
                }

                public interface ICheckoutPricer : IPricer
                {
                    decimal Discount();
                }

                public sealed class DefaultPricer : ICheckoutPricer
                {
                    decimal IPricer.Price() => 12m;

                    public decimal Discount() => 1m;
                }

                public sealed class Checkout(ICheckoutPricer pricer)
                {
                    public ICheckoutPricer Pricer { get; } = pricer;

                    public decimal Total() => Pricer.Price() - Pricer.Discount();
                }
                """)
            ]);

        var analyzer = new SingleImplementationInterfaceAnalyzer();
        var diagnostics = await GetAnalyzerDiagnosticsAsync(project, analyzer);
        var diagnostic = Assert.Single(diagnostics.Where(static diagnostic => diagnostic.GetMessage().Contains("IPricer", StringComparison.Ordinal)));
        var codeFix = new SingleImplementationInterfaceCodeFixProvider();
        var document = project.Documents.Single(static document => document.FilePath == "/repo/Feature.cs");

        var appliedFix = await ApplyCodeFixAsync(document, diagnostic, codeFix);
        var updatedProject = appliedFix.ChangedSolution.GetProject(project.Id)!;
        var updatedSource = (await updatedProject.Documents.Single(static document => document.FilePath == "/repo/Feature.cs").GetTextAsync()).ToString();
        var dependentInterfaceStart = updatedSource.IndexOf("public interface ICheckoutPricer", StringComparison.Ordinal);
        var implementationStart = updatedSource.IndexOf("public sealed class DefaultPricer", StringComparison.Ordinal);
        Assert.True(dependentInterfaceStart >= 0 && implementationStart > dependentInterfaceStart);
        var dependentInterfaceSource = updatedSource[dependentInterfaceStart..implementationStart];

        Assert.DoesNotContain("interface IPricer", updatedSource, StringComparison.Ordinal);
        Assert.Contains("public interface ICheckoutPricer", dependentInterfaceSource, StringComparison.Ordinal);
        Assert.DoesNotContain(": IPricer", dependentInterfaceSource, StringComparison.Ordinal);
        Assert.Contains("decimal Discount();", dependentInterfaceSource, StringComparison.Ordinal);
        Assert.Contains("decimal Price();", dependentInterfaceSource, StringComparison.Ordinal);
        Assert.DoesNotContain("decimal IPricer.Price()", updatedSource, StringComparison.Ordinal);
        Assert.Contains("public decimal Price() => 12m;", updatedSource, StringComparison.Ordinal);

        var compilationDiagnostics = await GetCompilationDiagnosticsAsync(updatedProject);
        Assert.Empty(compilationDiagnostics);
    }

    [Fact]
    public async Task UnusedDependencyAnalyzer_ReportsReferencedPackageWithoutSymbolUsage()
    {
        const string projectPath = "/repo/Demo.csproj";
        const string projectText = """
        <Project Sdk="Microsoft.NET.Sdk">
          <ItemGroup>
            <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
            <PackageReference Include="Humanizer.Core" Version="2.14.1" />
          </ItemGroup>
        </Project>
        """;

        var diagnostics = await RunAsync(
            new UnusedDependencyAnalyzer(),
            [
                new SourceFile("/repo/Feature.cs", """
                using Humanizer;

                namespace Demo;

                public static class Feature
                {
                    public static string Describe(int count) => count.ToWords();
                }
                """)
            ],
            additionalReferences:
            [
                CreateNuGetPackageReference("humanizer.core", "Humanizer.dll"),
                CreateNuGetPackageReference("newtonsoft.json", "Newtonsoft.Json.dll")
            ],
            additionalFiles: [CreateAdditionalText(projectPath, projectText)],
            globalOptions: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["build_property.MSBuildProjectFullPath"] = projectPath
            });

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(UnusedDependencyAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("Newtonsoft.Json", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Equal(projectPath, diagnostic.Location.SourceTree?.FilePath ?? diagnostic.Location.GetLineSpan().Path);
    }

    [Fact]
    public async Task UnusedDependencyAnalyzer_DoesNotReportUsedPackageReference()
    {
        const string projectPath = "/repo/Demo.csproj";
        const string projectText = """
        <Project Sdk="Microsoft.NET.Sdk">
          <ItemGroup>
            <PackageReference Include="Humanizer.Core" Version="2.14.1" />
          </ItemGroup>
        </Project>
        """;

        var diagnostics = await RunAsync(
            new UnusedDependencyAnalyzer(),
            [
                new SourceFile("/repo/Feature.cs", """
                using Humanizer;

                namespace Demo;

                public static class Feature
                {
                    public static string Describe(int count) => count.ToWords();
                }
                """)
            ],
            additionalReferences: [CreateNuGetPackageReference("humanizer.core", "Humanizer.dll")],
            additionalFiles: [CreateAdditionalText(projectPath, projectText)],
            globalOptions: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["build_property.MSBuildProjectFullPath"] = projectPath
            });

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task UnusedDependencyCodeFixProvider_RemovesPackageReferenceWithPreviewSafeRewrite()
    {
        const string projectPath = "/repo/Demo.csproj";
        const string projectText = """
        <Project Sdk="Microsoft.NET.Sdk">
          <ItemGroup>
            <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
            <PackageReference Include="Humanizer.Core" Version="2.14.1" />
          </ItemGroup>
        </Project>
        """;

        var project = CreateProject(
            [
                new SourceFile("/repo/Feature.cs", """
                using Humanizer;

                namespace Demo;

                public static class Feature
                {
                    public static string Describe(int count) => count.ToWords();
                }
                """)
            ],
            additionalReferences:
            [
                CreateNuGetPackageReference("humanizer.core", "Humanizer.dll"),
                CreateNuGetPackageReference("newtonsoft.json", "Newtonsoft.Json.dll")
            ],
            additionalDocuments:
            [
                new AdditionalDocumentFile(projectPath, projectText)
            ]);

        var analyzer = new UnusedDependencyAnalyzer();
        var diagnostics = await GetAnalyzerDiagnosticsAsync(
            project,
            analyzer,
            additionalFiles: [CreateAdditionalText(projectPath, projectText)],
            globalOptions: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["build_property.MSBuildProjectFullPath"] = projectPath
            });

        var diagnostic = Assert.Single(diagnostics);
        var codeFix = new UnusedDependencyCodeFixProvider();
        var textDocument = project.AdditionalDocuments.Single(static document => document.FilePath == projectPath);

        var appliedFix = await ApplyCodeFixAsync(textDocument, diagnostic, codeFix);
        Assert.Contains(appliedFix.PreviewOperations, static operation => operation is ApplyChangesOperation);

        var updatedProject = appliedFix.ChangedSolution.GetProject(project.Id)!;
        var updatedProjectText = (await updatedProject.AdditionalDocuments.Single(static document => document.FilePath == projectPath).GetTextAsync()).ToString();

        Assert.DoesNotContain("Newtonsoft.Json", updatedProjectText, StringComparison.Ordinal);
        Assert.Contains("""<PackageReference Include="Humanizer.Core" Version="2.14.1" />""", updatedProjectText, StringComparison.Ordinal);
        _ = System.Xml.Linq.XDocument.Parse(updatedProjectText);

        var updatedDiagnostics = await GetAnalyzerDiagnosticsAsync(
            updatedProject,
            analyzer,
            additionalFiles: [CreateAdditionalText(projectPath, updatedProjectText)],
            globalOptions: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["build_property.MSBuildProjectFullPath"] = projectPath
            });

        Assert.Empty(updatedDiagnostics);
    }

    [Fact]
    public async Task UnusedDependencyCodeFixProvider_RemovesMultilinePackageReferenceWithoutBreakingXml()
    {
        const string projectPath = "/repo/Demo.csproj";
        const string projectText = """
        <Project Sdk="Microsoft.NET.Sdk">
          <ItemGroup>
            <PackageReference Include="Newtonsoft.Json">
              <Version>13.0.3</Version>
              <PrivateAssets>all</PrivateAssets>
            </PackageReference>
            <PackageReference Include="Humanizer.Core" Version="2.14.1" />
          </ItemGroup>
        </Project>
        """;

        var project = CreateProject(
            [
                new SourceFile("/repo/Feature.cs", """
                using Humanizer;

                namespace Demo;

                public static class Feature
                {
                    public static string Describe(int count) => count.ToWords();
                }
                """)
            ],
            additionalReferences:
            [
                CreateNuGetPackageReference("humanizer.core", "Humanizer.dll"),
                CreateNuGetPackageReference("newtonsoft.json", "Newtonsoft.Json.dll")
            ],
            additionalDocuments:
            [
                new AdditionalDocumentFile(projectPath, projectText)
            ]);

        var analyzer = new UnusedDependencyAnalyzer();
        var diagnostics = await GetAnalyzerDiagnosticsAsync(
            project,
            analyzer,
            additionalFiles: [CreateAdditionalText(projectPath, projectText)],
            globalOptions: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["build_property.MSBuildProjectFullPath"] = projectPath
            });

        var diagnostic = Assert.Single(diagnostics);
        var codeFix = new UnusedDependencyCodeFixProvider();
        var textDocument = project.AdditionalDocuments.Single(static document => document.FilePath == projectPath);

        var appliedFix = await ApplyCodeFixAsync(textDocument, diagnostic, codeFix);
        Assert.Contains(appliedFix.PreviewOperations, static operation => operation is ApplyChangesOperation);

        var updatedProject = appliedFix.ChangedSolution.GetProject(project.Id)!;
        var updatedProjectText = (await updatedProject.AdditionalDocuments.Single(static document => document.FilePath == projectPath).GetTextAsync()).ToString();

        Assert.DoesNotContain("Newtonsoft.Json", updatedProjectText, StringComparison.Ordinal);
        Assert.DoesNotContain("PrivateAssets", updatedProjectText, StringComparison.Ordinal);
        Assert.Contains("""<PackageReference Include="Humanizer.Core" Version="2.14.1" />""", updatedProjectText, StringComparison.Ordinal);
        _ = System.Xml.Linq.XDocument.Parse(updatedProjectText);

        var updatedDiagnostics = await GetAnalyzerDiagnosticsAsync(
            updatedProject,
            analyzer,
            additionalFiles: [CreateAdditionalText(projectPath, updatedProjectText)],
            globalOptions: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["build_property.MSBuildProjectFullPath"] = projectPath
            });

        Assert.Empty(updatedDiagnostics);
    }

    [Fact]
    public async Task HighComplexityAnalyzer_ReportsMethodAboveThreshold()
    {
        var diagnostics = await RunAsync(
            new HighComplexityAnalyzer(),
            [
                new SourceFile("/repo/Complexity.cs", """
                namespace Demo;

                public sealed class Feature
                {
                    public int Score(bool a, bool b, bool c, bool d, bool e, bool f, bool g, bool h, bool i, bool j, bool k)
                    {
                        var result = 0;
                        if (a) result++;
                        if (b) result++;
                        if (c) result++;
                        if (d) result++;
                        if (e) result++;
                        if (f) result++;
                        if (g) result++;
                        if (h) result++;
                        if (i) result++;
                        if (j) result++;
                        if (k) result++;
                        return result;
                    }
                }
                """)
            ]);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(HighComplexityAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("complexity 12", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HighComplexityAnalyzer_DoesNotReportSimpleMethod()
    {
        var diagnostics = await RunAsync(
            new HighComplexityAnalyzer(),
            [
                new SourceFile("/repo/Complexity.cs", """
                namespace Demo;

                public sealed class Feature
                {
                    public int Score(bool flag) => flag ? 1 : 0;
                }
                """)
            ]);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AbstractionLayerDepthAnalyzer_ReportsLongSourceCallChain()
    {
        var source = """
        namespace Demo;

        public sealed class Pipeline
        {
            public void Step1() => Step2();
            private void Step2() => Step3();
            private void Step3() => Step4();
            private void Step4() => Step5();
            private void Step5() => Step6();
            private void Step6() => Step7();
            private void Step7() => Step8();
            private void Step8() => Step9();
            private void Step9() => Step10();
            private void Step10() { }
        }
        """;

        var diagnostics = await RunAsync(
            new AbstractionLayerDepthAnalyzer(),
            [new SourceFile("/repo/Pipeline.cs", source)]);

        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == AbstractionLayerDepthAnalyzer.DiagnosticId && diagnostic.GetMessage().Contains("Step1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AbstractionLayerDepthAnalyzer_DoesNotReportShortChain()
    {
        var diagnostics = await RunAsync(
            new AbstractionLayerDepthAnalyzer(),
            [
                new SourceFile("/repo/Pipeline.cs", """
                namespace Demo;

                public sealed class Pipeline
                {
                    public void Step1() => Step2();
                    private void Step2() => Step3();
                    private void Step3() => Step4();
                    private void Step4() { }
                }
                """)
            ]);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ConstructorParameterCountAnalyzer_ReportsConstructorAboveThreshold()
    {
        var diagnostics = await RunAsync(
            new ConstructorParameterCountAnalyzer(),
            [
                new SourceFile("/repo/Feature.cs", """
                namespace Demo;

                public sealed class Feature(
                    int a,
                    int b,
                    int c,
                    int d,
                    int e,
                    int f,
                    int g,
                    int h)
                {
                }
                """)
            ]);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(ConstructorParameterCountAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("8 parameters", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConstructorParameterCountAnalyzer_DoesNotReportConstructorWithinThreshold()
    {
        var diagnostics = await RunAsync(
            new ConstructorParameterCountAnalyzer(),
            [
                new SourceFile("/repo/Feature.cs", """
                namespace Demo;

                public sealed class Feature(int a, int b, int c, int d, int e, int f, int g)
                {
                }
                """)
            ]);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ConstructorParameterCountAnalyzer_DoesNotReportStructPrimaryConstructorAboveThreshold()
    {
        var diagnostics = await RunAsync(
            new ConstructorParameterCountAnalyzer(),
            [
                new SourceFile("/repo/Feature.cs", """
                namespace Demo;

                public readonly record struct Feature(
                    int a,
                    int b,
                    int c,
                    int d,
                    int e,
                    int f,
                    int g,
                    int h);
                """)
            ]);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task SingleSpecializationGenericParameterAnalyzer_ReportsSingleConcreteTypeArgument()
    {
        var diagnostics = await RunAsync(
            new SingleSpecializationGenericParameterAnalyzer(),
            [
                new SourceFile("/repo/Feature.cs", """
                namespace Demo;

                public sealed class Envelope<T>
                {
                    public T Value { get; }

                    public Envelope(T value)
                    {
                        Value = value;
                    }
                }

                public static class Usage
                {
                    public static Envelope<string> Create() => new("hello");
                }
                """)
            ]);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(SingleSpecializationGenericParameterAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("Envelope<T>", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("string", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SingleSpecializationGenericParameterAnalyzer_DoesNotReportMultipleConcreteTypeArguments()
    {
        var diagnostics = await RunAsync(
            new SingleSpecializationGenericParameterAnalyzer(),
            [
                new SourceFile("/repo/Feature.cs", """
                namespace Demo;

                public sealed class Envelope<T>
                {
                    public T Value { get; }

                    public Envelope(T value)
                    {
                        Value = value;
                    }
                }

                public static class Usage
                {
                    public static Envelope<string> CreateName() => new("hello");
                    public static Envelope<int> CreateNumber() => new(42);
                }
                """)
            ]);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NonPrimaryPathOverReferencedAnalyzer_ReportsSupportFileThatOutdrawsPrimaryPath()
    {
        var diagnostics = await RunAsync(
            new NonPrimaryPathOverReferencedAnalyzer(),
            [
                new SourceFile("/repo/SimplicityTools.Metrics.PrimaryPathAttribute.cs", """
                namespace SimplicityTools.Metrics;

                [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
                public sealed class PrimaryPathAttribute : System.Attribute
                {
                }
                """),
                new SourceFile("/repo/Controllers/CheckoutController.cs", """
                namespace Demo.Controllers;

                [SimplicityTools.Metrics.PrimaryPath]
                public sealed class CheckoutController
                {
                }
                """),
                new SourceFile("/repo/Services/OrderCoordinator.cs", """
                namespace Demo.Services;

                public sealed class OrderCoordinator
                {
                }
                """),
                new SourceFile("/repo/Consumers.cs", """
                namespace Demo;

                public sealed class ConsumerA
                {
                    private readonly Services.OrderCoordinator _coordinator = new();
                }

                public sealed class ConsumerB
                {
                    private readonly Services.OrderCoordinator _coordinator = new();
                }

                public sealed class ConsumerC
                {
                    private readonly Services.OrderCoordinator _coordinator = new();
                }

                public sealed class ControllerConsumer
                {
                    private readonly Controllers.CheckoutController _controller = new();
                }
                """)
            ]);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(NonPrimaryPathOverReferencedAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("OrderCoordinator.cs", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task NonPrimaryPathOverReferencedAnalyzer_DoesNotReportWhenPrimaryPathStaysMostReferenced()
    {
        var diagnostics = await RunAsync(
            new NonPrimaryPathOverReferencedAnalyzer(),
            [
                new SourceFile("/repo/SimplicityTools.Metrics.PrimaryPathAttribute.cs", """
                namespace SimplicityTools.Metrics;

                [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
                public sealed class PrimaryPathAttribute : System.Attribute
                {
                }
                """),
                new SourceFile("/repo/Controllers/CheckoutController.cs", """
                namespace Demo.Controllers;

                [SimplicityTools.Metrics.PrimaryPath]
                public sealed class CheckoutController
                {
                }
                """),
                new SourceFile("/repo/Services/OrderCoordinator.cs", """
                namespace Demo.Services;

                public sealed class OrderCoordinator
                {
                }
                """),
                new SourceFile("/repo/Consumers.cs", """
                namespace Demo;

                public sealed class ConsumerA
                {
                    private readonly Controllers.CheckoutController _controller = new();
                }

                public sealed class ConsumerB
                {
                    private readonly Controllers.CheckoutController _controller = new();
                }

                public sealed class ConsumerC
                {
                    private readonly Controllers.CheckoutController _controller = new();
                }

                public sealed class ServiceConsumer
                {
                    private readonly Services.OrderCoordinator _coordinator = new();
                }
                """)
            ]);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NonPrimaryPathOverReferencedAnalyzer_TreatsConventionalFilesAsSupportingWhenAnnotationsExist()
    {
        var diagnostics = await RunAsync(
            new NonPrimaryPathOverReferencedAnalyzer(),
            [
                new SourceFile("/repo/SimplicityTools.Metrics.PrimaryPathAttribute.cs", """
                namespace SimplicityTools.Metrics;

                [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
                public sealed class PrimaryPathAttribute : System.Attribute
                {
                }
                """),
                new SourceFile("/repo/Controllers/CheckoutController.cs", """
                namespace Demo.Controllers;

                [SimplicityTools.Metrics.PrimaryPath]
                public sealed class CheckoutController
                {
                }
                """),
                new SourceFile("/repo/Handlers/AuditHandler.cs", """
                namespace Demo.Handlers;

                public sealed class AuditHandler
                {
                }
                """),
                new SourceFile("/repo/Consumers.cs", """
                namespace Demo;

                public sealed class ControllerConsumer
                {
                    private readonly Controllers.CheckoutController _controller = new();
                }

                public sealed class AuditConsumerA
                {
                    private readonly Handlers.AuditHandler _handler = new();
                }

                public sealed class AuditConsumerB
                {
                    private readonly Handlers.AuditHandler _handler = new();
                }
                """)
            ]);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(NonPrimaryPathOverReferencedAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("AuditHandler.cs", diagnostic.GetMessage(), StringComparison.Ordinal);
    }
}
