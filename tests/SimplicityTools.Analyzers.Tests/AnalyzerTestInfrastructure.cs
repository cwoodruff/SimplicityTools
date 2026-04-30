using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace SimplicityTools.Analyzers.Tests;

internal static class AnalyzerTestInfrastructure
{
    public static async Task<ImmutableArray<Diagnostic>> RunAsync(
        DiagnosticAnalyzer analyzer,
        IEnumerable<SourceFile> sourceFiles,
        IEnumerable<MetadataReference>? additionalReferences = null,
        IEnumerable<AdditionalText>? additionalFiles = null,
        IReadOnlyDictionary<string, string>? globalOptions = null,
        CancellationToken cancellationToken = default)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTrees = sourceFiles
            .Select(file => CSharpSyntaxTree.ParseText(file.Text, parseOptions, file.Path))
            .ToArray();

        var references = GetDefaultReferences().Concat(additionalReferences ?? []).Distinct(MetadataReferencePathComparer.Instance).ToArray();
        var compilation = CSharpCompilation.Create(
            assemblyName: "AnalyzerTests",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzerOptions = new AnalyzerOptions(
            additionalFiles?.ToImmutableArray() ?? [],
            new TestAnalyzerConfigOptionsProvider(globalOptions));
        var options = new CompilationWithAnalyzersOptions(
            analyzerOptions,
            onAnalyzerException: null,
            concurrentAnalysis: false,
            logAnalyzerExecutionTime: false,
            reportSuppressedDiagnostics: false);

        var diagnostics = await compilation.WithAnalyzers([analyzer], options)
            .GetAnalyzerDiagnosticsAsync(cancellationToken)
            .ConfigureAwait(false);

        return diagnostics.OrderBy(static diagnostic => diagnostic.Id, StringComparer.Ordinal)
            .ThenBy(static diagnostic => diagnostic.Location.SourceTree?.FilePath, StringComparer.Ordinal)
            .ThenBy(static diagnostic => diagnostic.Location.SourceSpan.Start)
            .ToImmutableArray();
    }

    public static MetadataReference CreateReference<T>()
        => MetadataReference.CreateFromFile(typeof(T).Assembly.Location);

    public static MetadataReference CreateReference(Type type)
        => MetadataReference.CreateFromFile(type.Assembly.Location);

    public static MetadataReference CreateNuGetPackageReference(string packageId, string assemblyFileName)
    {
        var packageRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget",
            "packages",
            packageId.ToLowerInvariant());
        var assemblyPath = Directory.GetFiles(packageRoot, assemblyFileName, SearchOption.AllDirectories)
            .OrderByDescending(static path => path.Contains($"{Path.DirectorySeparatorChar}net10.0{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(static path => path.Contains($"{Path.DirectorySeparatorChar}net8.0{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(static path => path.Contains($"{Path.DirectorySeparatorChar}net6.0{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(static path => path.Contains($"{Path.DirectorySeparatorChar}netstandard2.0{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .First();

        return MetadataReference.CreateFromFile(assemblyPath);
    }

    public static AdditionalText CreateAdditionalText(string path, string text)
        => new InMemoryAdditionalText(path, text);

    public static Project CreateProject(
        IEnumerable<SourceFile> sourceFiles,
        IEnumerable<MetadataReference>? additionalReferences = null,
        IEnumerable<AdditionalDocumentFile>? additionalDocuments = null)
    {
        var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var solution = workspace.CurrentSolution.AddProject(
            projectId,
            "AnalyzerTests",
            "AnalyzerTests",
            LanguageNames.CSharp);

        solution = solution.WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        solution = solution.WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion.Preview));

        foreach (var reference in GetDefaultReferences().Concat(additionalReferences ?? []).Distinct(MetadataReferencePathComparer.Instance))
        {
            solution = solution.AddMetadataReference(projectId, reference);
        }

        foreach (var sourceFile in sourceFiles)
        {
            solution = solution.AddDocument(
                DocumentId.CreateNewId(projectId),
                Path.GetFileName(sourceFile.Path),
                SourceText.From(sourceFile.Text),
                filePath: sourceFile.Path);
        }

        foreach (var additionalDocument in additionalDocuments ?? [])
        {
            solution = solution.AddAdditionalDocument(
                DocumentId.CreateNewId(projectId),
                Path.GetFileName(additionalDocument.Path),
                SourceText.From(additionalDocument.Text),
                filePath: additionalDocument.Path);
        }

        if (!workspace.TryApplyChanges(solution))
        {
            throw new InvalidOperationException("Could not create analyzer test workspace.");
        }

        return workspace.CurrentSolution.GetProject(projectId)
            ?? throw new InvalidOperationException("Analyzer test project was not available.");
    }

    public static async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(
        Project project,
        DiagnosticAnalyzer analyzer,
        IEnumerable<AdditionalText>? additionalFiles = null,
        IReadOnlyDictionary<string, string>? globalOptions = null,
        CancellationToken cancellationToken = default)
    {
        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Compilation was not available.");

        var analyzerOptions = new AnalyzerOptions(
            additionalFiles?.ToImmutableArray() ?? [],
            new TestAnalyzerConfigOptionsProvider(globalOptions));
        var options = new CompilationWithAnalyzersOptions(
            analyzerOptions,
            onAnalyzerException: null,
            concurrentAnalysis: false,
            logAnalyzerExecutionTime: false,
            reportSuppressedDiagnostics: false);

        var diagnostics = await compilation.WithAnalyzers([analyzer], options)
            .GetAnalyzerDiagnosticsAsync(cancellationToken)
            .ConfigureAwait(false);

        return diagnostics.OrderBy(static diagnostic => diagnostic.Id, StringComparer.Ordinal)
            .ThenBy(static diagnostic => diagnostic.Location.SourceTree?.FilePath, StringComparer.Ordinal)
            .ThenBy(static diagnostic => diagnostic.Location.SourceSpan.Start)
            .ToImmutableArray();
    }

    public static async Task<AppliedCodeFix> ApplyCodeFixAsync(
        TextDocument textDocument,
        Diagnostic diagnostic,
        CodeFixProvider codeFixProvider,
        CancellationToken cancellationToken = default)
    {
        var actions = new List<CodeAction>();
        CodeFixContext context = textDocument switch
        {
            Document document => new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), cancellationToken),
            _ => new CodeFixContext(textDocument, diagnostic, (action, _) => actions.Add(action), cancellationToken)
        };

        await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
        var action = actions.Single();
        var previewOperations = await action.GetPreviewOperationsAsync(cancellationToken).ConfigureAwait(false);
        var applyChangesOperation = (await action.GetOperationsAsync(cancellationToken).ConfigureAwait(false))
            .OfType<ApplyChangesOperation>()
            .Single();

        return new AppliedCodeFix(action, previewOperations, applyChangesOperation.ChangedSolution);
    }

    public static async Task<ImmutableArray<Diagnostic>> GetCompilationDiagnosticsAsync(Project project, CancellationToken cancellationToken = default)
    {
        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Compilation was not available.");

        return compilation.GetDiagnostics(cancellationToken)
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();
    }

    private static IEnumerable<MetadataReference> GetDefaultReferences()
    {
        var trustedPlatformAssemblies = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)
            ?.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            ?? throw new InvalidOperationException("Trusted platform assemblies were not available.");

        foreach (var assemblyPath in trustedPlatformAssemblies)
        {
            yield return MetadataReference.CreateFromFile(assemblyPath);
        }
    }

    internal sealed record SourceFile(string Path, string Text);

    internal sealed record AdditionalDocumentFile(string Path, string Text);

    internal sealed record AppliedCodeFix(CodeAction Action, ImmutableArray<CodeActionOperation> PreviewOperations, Solution ChangedSolution);

    private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
    {
        private readonly SourceText _sourceText = SourceText.From(text);

        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default) => _sourceText;
    }

    private sealed class MetadataReferencePathComparer : IEqualityComparer<MetadataReference>
    {
        public static MetadataReferencePathComparer Instance { get; } = new();

        public bool Equals(MetadataReference? x, MetadataReference? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            return x?.Display == y?.Display;
        }

        public int GetHashCode(MetadataReference obj)
            => StringComparer.Ordinal.GetHashCode(obj.Display ?? string.Empty);
    }

    private sealed class TestAnalyzerConfigOptionsProvider(IReadOnlyDictionary<string, string>? globalOptions)
        : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions _globalOptions = new TestAnalyzerConfigOptions(globalOptions);
        private readonly AnalyzerConfigOptions _empty = new TestAnalyzerConfigOptions(null);

        public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _empty;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _empty;
    }

    private sealed class TestAnalyzerConfigOptions(IReadOnlyDictionary<string, string>? values) : AnalyzerConfigOptions
    {
        private readonly IReadOnlyDictionary<string, string> _values = values ?? new Dictionary<string, string>(StringComparer.Ordinal);

        public override bool TryGetValue(string key, out string value)
            => _values.TryGetValue(key, out value!);
    }
}
