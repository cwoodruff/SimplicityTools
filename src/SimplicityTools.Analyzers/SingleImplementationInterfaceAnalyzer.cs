using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SimplicityTools.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SingleImplementationInterfaceAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SF0001";

    /// <summary>
    /// Diagnostic property key carrying the documentation-comment declaration id of the single
    /// concrete implementation, so the code fix can resolve it without re-indexing the compilation.
    /// </summary>
    public const string ImplementationIdPropertyName = "ImplementationId";

    public static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Interface has single implementation",
        messageFormat: "Interface {0} has exactly one non-abstract implementation: {1}. Remove the interface and use the concrete type directly.",
        category: AnalyzerCategories.HalfRule,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: "https://simplicitytools.dev/analyzers/sf0001/",
        description: "Interfaces with a single concrete implementation add indirection without buying polymorphism.",
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static startContext =>
        {
            var interfaces = new ConcurrentDictionary<INamedTypeSymbol, byte>(SymbolEqualityComparer.Default);
            var implementations = new ConcurrentDictionary<INamedTypeSymbol, ImplementationAccumulator>(SymbolEqualityComparer.Default);

            startContext.RegisterSymbolAction(
                symbolContext => Collect(symbolContext, interfaces, implementations),
                SymbolKind.NamedType);
            startContext.RegisterCompilationEndAction(
                endContext => Report(endContext, interfaces, implementations));
        });
    }

    private static void Collect(
        SymbolAnalysisContext context,
        ConcurrentDictionary<INamedTypeSymbol, byte> interfaces,
        ConcurrentDictionary<INamedTypeSymbol, ImplementationAccumulator> implementations)
    {
        if (context.Symbol is not INamedTypeSymbol namedType ||
            !namedType.Locations.Any(static location => location.IsInSource))
        {
            return;
        }

        if (namedType.TypeKind == TypeKind.Interface)
        {
            interfaces.TryAdd(namedType, 0);
            return;
        }

        if (namedType.TypeKind is not (TypeKind.Class or TypeKind.Struct) ||
            namedType.IsAbstract ||
            !namedType.DeclaringSyntaxReferences.Any(static reference =>
                AnalyzerSourceFileConventions.IsCountableSourceFile(reference.SyntaxTree.FilePath)))
        {
            return;
        }

        var implementedDefinitions = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var implementedInterface in namedType.AllInterfaces)
        {
            if (implementedDefinitions.Add(implementedInterface.OriginalDefinition))
            {
                implementations
                    .GetOrAdd(implementedInterface.OriginalDefinition, static _ => new ImplementationAccumulator())
                    .Add(namedType);
            }
        }
    }

    private static void Report(
        CompilationAnalysisContext context,
        ConcurrentDictionary<INamedTypeSymbol, byte> interfaces,
        ConcurrentDictionary<INamedTypeSymbol, ImplementationAccumulator> implementations)
    {
        foreach (var interfaceSymbol in interfaces.Keys)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (SymbolVisibility.IsExternallyVisible(interfaceSymbol) &&
                !AnalyzerOptionReader.GetFlag(
                    context.Options,
                    AnalyzerOptionReader.GetDeclaringTree(interfaceSymbol),
                    AnalyzerOptionReader.IncludePublicApiKey,
                    defaultValue: false))
            {
                continue;
            }

            if (!implementations.TryGetValue(interfaceSymbol.OriginalDefinition, out var accumulator) ||
                accumulator.SingleImplementation is not { } implementation)
            {
                continue;
            }

            var properties = ImmutableDictionary<string, string?>.Empty
                .Add(ImplementationIdPropertyName, DocumentationCommentId.CreateDeclarationId(implementation));
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                interfaceSymbol.Locations[0],
                properties: properties,
                messageArgs:
                [
                    interfaceSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    implementation.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
                ]));
        }
    }

    private sealed class ImplementationAccumulator
    {
        private readonly object _gate = new();
        private INamedTypeSymbol? _first;
        private int _count;

        public void Add(INamedTypeSymbol implementation)
        {
            lock (_gate)
            {
                _count++;
                _first ??= implementation;
            }
        }

        public INamedTypeSymbol? SingleImplementation
        {
            get
            {
                lock (_gate)
                {
                    return _count == 1 ? _first : null;
                }
            }
        }
    }
}
