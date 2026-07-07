using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SimplicityTools.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SingleSpecializationGenericParameterAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SF0006";

    public static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Generic parameter has only one specialization",
        messageFormat: "Generic parameter {0} on {1} is only specialized as {2}. Remove the generic parameter or use the concrete type directly.",
        category: AnalyzerCategories.HalfRule,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: "https://simplicitytools.dev/analyzers/sf0006/",
        description: "A generic parameter that is only ever bound to one concrete type is indirection without flexibility.",
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static startContext =>
        {
            var genericDefinitions = new ConcurrentDictionary<ISymbol, ImmutableArray<ITypeParameterSymbol>>(SymbolEqualityComparer.Default);
            var specializations = new ConcurrentDictionary<ISymbol, ConcurrentDictionary<int, ConcurrentDictionary<ITypeSymbol, byte>>>(SymbolEqualityComparer.Default);

            startContext.RegisterSymbolAction(
                symbolContext => CollectGenericDefinition(symbolContext.Symbol, genericDefinitions),
                SymbolKind.NamedType,
                SymbolKind.Method);
            startContext.RegisterSemanticModelAction(semanticModelContext =>
            {
                var syntaxTree = semanticModelContext.SemanticModel.SyntaxTree;
                if (!AnalyzerSourceFileConventions.IsCountableSourceFile(syntaxTree.FilePath))
                {
                    return;
                }

                CollectSpecializations(semanticModelContext.SemanticModel, specializations, semanticModelContext.CancellationToken);
            });
            startContext.RegisterCompilationEndAction(
                endContext => Report(endContext, genericDefinitions, specializations));
        });
    }

    private static void CollectGenericDefinition(
        ISymbol symbol,
        ConcurrentDictionary<ISymbol, ImmutableArray<ITypeParameterSymbol>> genericDefinitions)
    {
        // Mirrors the previous SourceSymbolIndex population: named types declared by a
        // BaseTypeDeclarationSyntax (which excludes delegates) and ordinary methods, both in
        // countable source files.
        switch (symbol)
        {
            case INamedTypeSymbol namedType when namedType.Arity > 0 &&
                                                 namedType.TypeKind != TypeKind.Delegate &&
                                                 IsDeclaredInCountableFile(namedType):
                genericDefinitions.TryAdd(namedType, namedType.TypeParameters);
                break;
            case IMethodSymbol method when method.Arity > 0 &&
                                           method.MethodKind == MethodKind.Ordinary &&
                                           IsDeclaredInCountableFile(method):
                genericDefinitions.TryAdd(method, method.TypeParameters);
                break;
        }
    }

    private static bool IsDeclaredInCountableFile(ISymbol symbol)
        => symbol.DeclaringSyntaxReferences.Any(static reference =>
            AnalyzerSourceFileConventions.IsCountableSourceFile(reference.SyntaxTree.FilePath));

    private static void Report(
        CompilationAnalysisContext context,
        ConcurrentDictionary<ISymbol, ImmutableArray<ITypeParameterSymbol>> genericDefinitions,
        ConcurrentDictionary<ISymbol, ConcurrentDictionary<int, ConcurrentDictionary<ITypeSymbol, byte>>> specializations)
    {
        foreach (var pair in genericDefinitions)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var owner = pair.Key;
            var typeParameters = pair.Value;
            if (!specializations.TryGetValue(owner, out var specializedTypesByParameter))
            {
                continue;
            }

            if (SymbolVisibility.IsExternallyVisible(owner) &&
                !AnalyzerOptionReader.GetFlag(
                    context.Options,
                    AnalyzerOptionReader.GetDeclaringTree(owner),
                    AnalyzerOptionReader.IncludePublicApiKey,
                    defaultValue: false))
            {
                continue;
            }

            for (var index = 0; index < typeParameters.Length; index++)
            {
                if (!specializedTypesByParameter.TryGetValue(index, out var specializedTypes) || specializedTypes.Count != 1)
                {
                    continue;
                }

                var specializedType = specializedTypes.Keys.Single();
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    typeParameters[index].Locations[0],
                    typeParameters[index].Name,
                    owner.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    specializedType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            }
        }
    }

    private static void CollectSpecializations(
        SemanticModel semanticModel,
        ConcurrentDictionary<ISymbol, ConcurrentDictionary<int, ConcurrentDictionary<ITypeSymbol, byte>>> specializations,
        CancellationToken cancellationToken)
    {
        var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
        foreach (var node in root.DescendantNodesAndSelf())
        {
            cancellationToken.ThrowIfCancellationRequested();

            RecordTypeSpecialization(semanticModel.GetSymbolInfo(node, cancellationToken).Symbol, specializations);
            RecordTypeSpecialization(semanticModel.GetTypeInfo(node, cancellationToken).Type, specializations);
        }
    }

    private static void RecordTypeSpecialization(
        object? symbolOrType,
        ConcurrentDictionary<ISymbol, ConcurrentDictionary<int, ConcurrentDictionary<ITypeSymbol, byte>>> specializations)
    {
        switch (symbolOrType)
        {
            case INamedTypeSymbol namedType when namedType.Arity > 0:
                RecordConstructedSymbol(namedType.OriginalDefinition, namedType.TypeArguments, specializations);
                break;
            case IMethodSymbol method when method.IsGenericMethod:
                RecordConstructedSymbol(method.OriginalDefinition, method.TypeArguments, specializations);
                break;
        }
    }

    private static void RecordConstructedSymbol(
        ISymbol originalDefinition,
        ImmutableArray<ITypeSymbol> typeArguments,
        ConcurrentDictionary<ISymbol, ConcurrentDictionary<int, ConcurrentDictionary<ITypeSymbol, byte>>> specializations)
    {
        // Symbol actions may not have populated the definitions map yet, so record any
        // source-declared generic definition and intersect with the collected definitions when
        // reporting at compilation end.
        if (!originalDefinition.Locations.Any(static location => location.IsInSource))
        {
            return;
        }

        var typeParameterCount = originalDefinition switch
        {
            INamedTypeSymbol namedType => namedType.TypeParameters.Length,
            IMethodSymbol method => method.TypeParameters.Length,
            _ => 0
        };

        for (var index = 0; index < Math.Min(typeParameterCount, typeArguments.Length); index++)
        {
            var typeArgument = typeArguments[index];
            if (typeArgument.TypeKind == TypeKind.TypeParameter)
            {
                continue;
            }

            specializations
                .GetOrAdd(originalDefinition, static _ => new ConcurrentDictionary<int, ConcurrentDictionary<ITypeSymbol, byte>>())
                .GetOrAdd(index, static _ => new ConcurrentDictionary<ITypeSymbol, byte>(SymbolEqualityComparer.Default))
                .TryAdd(typeArgument, 0);
        }
    }
}
