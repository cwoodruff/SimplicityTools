using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SimplicityTools.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NonPrimaryPathOverReferencedAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SF0007";

    public static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Supporting file is referenced more than the primary path",
        messageFormat: "File {0} has {1} inbound references, exceeding the highest primary-path file count of {2}",
        category: AnalyzerCategories.PrimaryPathFirst,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://simplicity-first.dev/analyzers/SF0007",
        description: "When supporting files become more referenced than primary-path files, the real business flow is no longer obvious.",
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(Analyze);
    }

    private static void Analyze(CompilationAnalysisContext context)
    {
        var documentInfoByPath = CollectDocumentInfo(context.Compilation, context.CancellationToken);
        if (documentInfoByPath.Count == 0)
        {
            return;
        }

        var primaryPaths = documentInfoByPath.Values.Any(static info => info.IsAnnotatedPrimaryPath)
            ? documentInfoByPath.Values.Where(static info => info.IsAnnotatedPrimaryPath).ToArray()
            : documentInfoByPath.Values.Where(static info => info.MatchesPrimaryPathConvention).ToArray();

        if (primaryPaths.Length == 0)
        {
            return;
        }

        var highestPrimaryPathReferenceCount = primaryPaths.Max(static info => info.InboundReferenceCount);
        if (highestPrimaryPathReferenceCount <= 0)
        {
            return;
        }

        foreach (var documentInfo in documentInfoByPath.Values)
        {
            var isPrimaryPath = primaryPaths.Contains(documentInfo);
            if (isPrimaryPath ||
                documentInfo.InboundReferenceCount <= highestPrimaryPathReferenceCount)
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                documentInfo.Location,
                Path.GetFileName(documentInfo.FilePath),
                documentInfo.InboundReferenceCount,
                highestPrimaryPathReferenceCount));
        }
    }

    private static ImmutableDictionary<string, DocumentInfo> CollectDocumentInfo(Compilation compilation, CancellationToken cancellationToken)
    {
        var sourceIndex = SourceSymbolIndex.Create(compilation, cancellationToken);
        var declaredTypesByFile = new Dictionary<string, HashSet<INamedTypeSymbol>>(StringComparer.OrdinalIgnoreCase);

        foreach (var namedType in sourceIndex.NamedTypes)
        {
            if (!sourceIndex.TryGetFilePath(namedType, out var filePath))
            {
                continue;
            }

            if (!declaredTypesByFile.TryGetValue(filePath, out var declaredTypes))
            {
                declaredTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                declaredTypesByFile[filePath] = declaredTypes;
            }

            declaredTypes.Add(namedType);
        }

        var inboundReferenceCounts = declaredTypesByFile.Keys.ToDictionary(path => path, _ => 0, StringComparer.OrdinalIgnoreCase);
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!AnalyzerSourceFileConventions.IsCountableSourceFile(syntaxTree.FilePath))
            {
                continue;
            }

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot(cancellationToken);
            foreach (var node in root.DescendantNodesAndSelf())
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var referencedType in CollectReferencedTypes(node, semanticModel, cancellationToken))
                {
                    if (!sourceIndex.TryGetFilePath(referencedType, out var targetPath) ||
                        string.Equals(targetPath, syntaxTree.FilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    inboundReferenceCounts[targetPath]++;
                }
            }
        }

        var builder = ImmutableDictionary.CreateBuilder<string, DocumentInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!AnalyzerSourceFileConventions.IsCountableSourceFile(syntaxTree.FilePath) ||
                !declaredTypesByFile.ContainsKey(syntaxTree.FilePath))
            {
                continue;
            }

            var root = syntaxTree.GetRoot(cancellationToken);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var location = root.GetLocation();
            builder[syntaxTree.FilePath] = new DocumentInfo(
                syntaxTree.FilePath,
                location,
                PrimaryPathConventions.IsPrimaryPathAnnotated(root, semanticModel, cancellationToken),
                PrimaryPathConventions.MatchesPrimaryPathConvention(syntaxTree.FilePath),
                inboundReferenceCounts[syntaxTree.FilePath]);
        }

        return builder.ToImmutable();
    }

    private static IEnumerable<INamedTypeSymbol> CollectReferencedTypes(
        SyntaxNode node,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var symbols = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        AddNamedType(semanticModel.GetSymbolInfo(node, cancellationToken).Symbol, symbols);
        foreach (var candidate in semanticModel.GetSymbolInfo(node, cancellationToken).CandidateSymbols)
        {
            AddNamedType(candidate, symbols);
        }

        var typeInfo = semanticModel.GetTypeInfo(node, cancellationToken);
        AddNamedType(typeInfo.Type, symbols);
        AddNamedType(typeInfo.ConvertedType, symbols);

        return symbols;
    }

    private static void AddNamedType(ISymbol? symbol, ISet<INamedTypeSymbol> symbols)
    {
        switch (symbol)
        {
            case null:
                return;
            case IAliasSymbol aliasSymbol:
                AddNamedType(aliasSymbol.Target, symbols);
                return;
            case INamedTypeSymbol namedType:
                symbols.Add(namedType.OriginalDefinition);
                return;
            case IMethodSymbol method when method.MethodKind == MethodKind.Constructor:
                symbols.Add(method.ContainingType.OriginalDefinition);
                return;
        }
    }

    private sealed record DocumentInfo(
        string FilePath,
        Location Location,
        bool IsAnnotatedPrimaryPath,
        bool MatchesPrimaryPathConvention,
        int InboundReferenceCount);
}
