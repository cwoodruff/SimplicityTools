using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        helpLinkUri: "https://simplicitytools.dev/analyzers/sf0007/",
        description: "When supporting files become more referenced than primary-path files, the real business flow is no longer obvious.",
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static startContext =>
        {
            var conventionFolders = AnalyzerOptionReader.GetNameList(
                startContext.Options,
                startContext.Compilation.SyntaxTrees.FirstOrDefault(),
                AnalyzerOptionReader.ConventionFoldersKey,
                PrimaryPathConventions.DefaultConventionalSegments);
            var typeFiles = new ConcurrentDictionary<INamedTypeSymbol, string>(SymbolEqualityComparer.Default);
            var treeInfoByPath = new ConcurrentDictionary<string, TreeInfo>(StringComparer.OrdinalIgnoreCase);
            var referencedTypeCountsByPath = new ConcurrentDictionary<string, Dictionary<INamedTypeSymbol, int>>(StringComparer.OrdinalIgnoreCase);

            startContext.RegisterSemanticModelAction(semanticModelContext =>
                CollectTree(semanticModelContext, conventionFolders, typeFiles, treeInfoByPath, referencedTypeCountsByPath));
            startContext.RegisterCompilationEndAction(endContext =>
                Report(endContext, typeFiles, treeInfoByPath, referencedTypeCountsByPath));
        });
    }

    private static void CollectTree(
        SemanticModelAnalysisContext context,
        ImmutableArray<string> conventionFolders,
        ConcurrentDictionary<INamedTypeSymbol, string> typeFiles,
        ConcurrentDictionary<string, TreeInfo> treeInfoByPath,
        ConcurrentDictionary<string, Dictionary<INamedTypeSymbol, int>> referencedTypeCountsByPath)
    {
        var semanticModel = context.SemanticModel;
        var syntaxTree = semanticModel.SyntaxTree;
        if (!AnalyzerSourceFileConventions.IsCountableSourceFile(syntaxTree.FilePath))
        {
            return;
        }

        var cancellationToken = context.CancellationToken;
        var root = syntaxTree.GetRoot(cancellationToken);

        var declaredAnyType = false;
        foreach (var typeDeclaration in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) is INamedTypeSymbol namedType)
            {
                typeFiles[namedType.OriginalDefinition] = syntaxTree.FilePath;
                declaredAnyType = true;
            }
        }

        var referencedTypeCounts = new Dictionary<INamedTypeSymbol, int>(SymbolEqualityComparer.Default);
        foreach (var node in root.DescendantNodesAndSelf())
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var referencedType in CollectReferencedTypes(node, semanticModel, cancellationToken))
            {
                referencedTypeCounts.TryGetValue(referencedType, out var count);
                referencedTypeCounts[referencedType] = count + 1;
            }
        }

        referencedTypeCountsByPath[syntaxTree.FilePath] = referencedTypeCounts;

        if (declaredAnyType)
        {
            treeInfoByPath[syntaxTree.FilePath] = new TreeInfo(
                syntaxTree.FilePath,
                GetReportLocation(root),
                PrimaryPathConventions.IsPrimaryPathAnnotated(root, semanticModel, cancellationToken),
                PrimaryPathConventions.MatchesPrimaryPathConvention(syntaxTree.FilePath, conventionFolders));
        }
    }

    private static void Report(
        CompilationAnalysisContext context,
        ConcurrentDictionary<INamedTypeSymbol, string> typeFiles,
        ConcurrentDictionary<string, TreeInfo> treeInfoByPath,
        ConcurrentDictionary<string, Dictionary<INamedTypeSymbol, int>> referencedTypeCountsByPath)
    {
        if (treeInfoByPath.IsEmpty)
        {
            return;
        }

        var inboundReferenceCounts = treeInfoByPath.Keys.ToDictionary(static path => path, static _ => 0, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in referencedTypeCountsByPath)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            foreach (var referencedTypeCount in pair.Value)
            {
                if (!typeFiles.TryGetValue(referencedTypeCount.Key, out var targetPath) ||
                    string.Equals(targetPath, pair.Key, StringComparison.OrdinalIgnoreCase) ||
                    !inboundReferenceCounts.ContainsKey(targetPath))
                {
                    continue;
                }

                inboundReferenceCounts[targetPath] += referencedTypeCount.Value;
            }
        }

        var documentInfoByPath = treeInfoByPath.Values
            .Select(info => new DocumentInfo(
                info.FilePath,
                info.Location,
                info.IsAnnotatedPrimaryPath,
                info.MatchesPrimaryPathConvention,
                inboundReferenceCounts[info.FilePath]))
            .ToArray();

        var primaryPaths = documentInfoByPath.Any(static info => info.IsAnnotatedPrimaryPath)
            ? documentInfoByPath.Where(static info => info.IsAnnotatedPrimaryPath).ToArray()
            : documentInfoByPath.Where(static info => info.MatchesPrimaryPathConvention).ToArray();

        if (primaryPaths.Length == 0)
        {
            return;
        }

        var highestPrimaryPathReferenceCount = primaryPaths.Max(static info => info.InboundReferenceCount);
        if (highestPrimaryPathReferenceCount <= 0)
        {
            return;
        }

        foreach (var documentInfo in documentInfoByPath)
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

    private static Location GetReportLocation(SyntaxNode root)
    {
        var firstTypeDeclaration = root.DescendantNodes()
            .OfType<BaseTypeDeclarationSyntax>()
            .FirstOrDefault();

        return firstTypeDeclaration?.Identifier.GetLocation() ?? root.GetLocation();
    }

    private static IEnumerable<INamedTypeSymbol> CollectReferencedTypes(
        SyntaxNode node,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var symbols = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        var symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
        AddNamedType(symbolInfo.Symbol, symbols);
        foreach (var candidate in symbolInfo.CandidateSymbols)
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

    private sealed record TreeInfo(
        string FilePath,
        Location Location,
        bool IsAnnotatedPrimaryPath,
        bool MatchesPrimaryPathConvention);

    private sealed record DocumentInfo(
        string FilePath,
        Location Location,
        bool IsAnnotatedPrimaryPath,
        bool MatchesPrimaryPathConvention,
        int InboundReferenceCount);
}
