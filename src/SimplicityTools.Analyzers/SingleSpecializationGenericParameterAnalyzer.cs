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
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://simplicity-first.dev/analyzers/SF0006",
        description: "A generic parameter that is only ever bound to one concrete type is indirection without flexibility.",
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
        var genericDefinitions = CollectGenericDefinitions(context.Compilation, context.CancellationToken);
        if (genericDefinitions.Count == 0)
        {
            return;
        }

        var specializations = CollectSpecializations(context.Compilation, genericDefinitions, context.CancellationToken);
        foreach (var pair in genericDefinitions)
        {
            var owner = pair.Key;
            var typeParameters = pair.Value;
            if (!specializations.TryGetValue(owner, out var specializedTypesByParameter))
            {
                continue;
            }

            for (var index = 0; index < typeParameters.Length; index++)
            {
                if (!specializedTypesByParameter.TryGetValue(index, out var specializedTypes) || specializedTypes.Count != 1)
                {
                    continue;
                }

                var specializedType = specializedTypes.Single();
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    typeParameters[index].Locations[0],
                    typeParameters[index].Name,
                    owner.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    specializedType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            }
        }
    }

    private static ImmutableDictionary<ISymbol, ImmutableArray<ITypeParameterSymbol>> CollectGenericDefinitions(
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        var builder = ImmutableDictionary.CreateBuilder<ISymbol, ImmutableArray<ITypeParameterSymbol>>(SymbolEqualityComparer.Default);
        var sourceIndex = SourceSymbolIndex.Create(compilation, cancellationToken);

        foreach (var namedType in sourceIndex.NamedTypes.Where(static type => type.Arity > 0))
        {
            builder[namedType] = [.. namedType.TypeParameters];
        }

        foreach (var method in sourceIndex.Methods.Where(static method => method.Arity > 0))
        {
            builder[method] = [.. method.TypeParameters];
        }

        return builder.ToImmutable();
    }

    private static ImmutableDictionary<ISymbol, ImmutableDictionary<int, ImmutableHashSet<ITypeSymbol>>> CollectSpecializations(
        Compilation compilation,
        ImmutableDictionary<ISymbol, ImmutableArray<ITypeParameterSymbol>> genericDefinitions,
        CancellationToken cancellationToken)
    {
        var builders = new Dictionary<ISymbol, Dictionary<int, HashSet<ITypeSymbol>>>(SymbolEqualityComparer.Default);

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

                RecordTypeSpecialization(semanticModel.GetSymbolInfo(node, cancellationToken).Symbol, genericDefinitions, builders);
                RecordTypeSpecialization(semanticModel.GetTypeInfo(node, cancellationToken).Type, genericDefinitions, builders);
            }
        }

        var result = ImmutableDictionary.CreateBuilder<ISymbol, ImmutableDictionary<int, ImmutableHashSet<ITypeSymbol>>>(SymbolEqualityComparer.Default);
        foreach (var pair in builders)
        {
            var parameterMap = ImmutableDictionary.CreateBuilder<int, ImmutableHashSet<ITypeSymbol>>();
            foreach (var entry in pair.Value)
            {
                parameterMap[entry.Key] = ImmutableHashSet.CreateRange<ITypeSymbol>(SymbolEqualityComparer.Default, entry.Value);
            }

            result[pair.Key] = parameterMap.ToImmutable();
        }

        return result.ToImmutable();
    }

    private static void RecordTypeSpecialization(
        object? symbolOrType,
        ImmutableDictionary<ISymbol, ImmutableArray<ITypeParameterSymbol>> genericDefinitions,
        IDictionary<ISymbol, Dictionary<int, HashSet<ITypeSymbol>>> builders)
    {
        switch (symbolOrType)
        {
            case INamedTypeSymbol namedType:
                RecordConstructedSymbol(namedType.OriginalDefinition, namedType.TypeArguments, genericDefinitions, builders);
                break;
            case IMethodSymbol method when method.IsGenericMethod:
                RecordConstructedSymbol(method.OriginalDefinition, method.TypeArguments, genericDefinitions, builders);
                break;
        }
    }

    private static void RecordConstructedSymbol(
        ISymbol originalDefinition,
        ImmutableArray<ITypeSymbol> typeArguments,
        ImmutableDictionary<ISymbol, ImmutableArray<ITypeParameterSymbol>> genericDefinitions,
        IDictionary<ISymbol, Dictionary<int, HashSet<ITypeSymbol>>> builders)
    {
        if (!genericDefinitions.TryGetValue(originalDefinition, out var typeParameters))
        {
            return;
        }

        for (var index = 0; index < Math.Min(typeParameters.Length, typeArguments.Length); index++)
        {
            var typeArgument = typeArguments[index];
            if (typeArgument.TypeKind == TypeKind.TypeParameter)
            {
                continue;
            }

            if (!builders.TryGetValue(originalDefinition, out var parameterMap))
            {
                parameterMap = [];
                builders[originalDefinition] = parameterMap;
            }

            if (!parameterMap.TryGetValue(index, out var specializedTypes))
            {
                specializedTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
                parameterMap[index] = specializedTypes;
            }

            specializedTypes.Add(typeArgument);
        }
    }
}
