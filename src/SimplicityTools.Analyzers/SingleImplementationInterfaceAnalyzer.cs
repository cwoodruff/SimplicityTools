using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SimplicityTools.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SingleImplementationInterfaceAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SF0001";

    public static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Interface has single implementation",
        messageFormat: "Interface {0} has exactly one non-abstract implementation: {1}. Remove the interface and use the concrete type directly.",
        category: AnalyzerCategories.HalfRule,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://simplicitytools.dev/analyzers/sf0001/",
        description: "Interfaces with a single concrete implementation add indirection without buying polymorphism.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static startContext =>
        {
            var concreteTypes = SourceSymbolIndex.Create(startContext.Compilation, startContext.CancellationToken)
                .NamedTypes
                .Where(static type => type.TypeKind is TypeKind.Class or TypeKind.Struct && !type.IsAbstract)
                .ToImmutableArray();

            startContext.RegisterSymbolAction(symbolContext => Analyze(symbolContext, concreteTypes), SymbolKind.NamedType);
        });
    }

    private static void Analyze(SymbolAnalysisContext context, ImmutableArray<INamedTypeSymbol> concreteTypes)
    {
        if (context.Symbol is not INamedTypeSymbol interfaceSymbol ||
            interfaceSymbol.TypeKind != TypeKind.Interface ||
            !interfaceSymbol.Locations.Any(static location => location.IsInSource))
        {
            return;
        }

        var interfaceDefinition = interfaceSymbol.OriginalDefinition;
        var implementations = concreteTypes
            .Where(type => type.AllInterfaces.Any(implemented => SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, interfaceDefinition)))
            .Take(2)
            .ToArray();

        if (implementations.Length != 1)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            interfaceSymbol.Locations[0],
            interfaceSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            implementations[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }
}
