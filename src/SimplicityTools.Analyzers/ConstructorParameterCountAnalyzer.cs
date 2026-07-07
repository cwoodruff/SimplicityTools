using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SimplicityTools.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConstructorParameterCountAnalyzer : DiagnosticAnalyzer
{
    internal const int DefaultParameterThreshold = 7;

    public const string DiagnosticId = "SF0005";

    public static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Constructor takes too many parameters",
        messageFormat: "Constructor on {0} takes {1} parameters, exceeding the limit of {2}",
        category: AnalyzerCategories.TwoAmTest,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://simplicitytools.dev/analyzers/sf0005/",
        description: "Large constructor parameter lists are a strong signal that a type is doing too much.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
    }

    private static void Analyze(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol namedType ||
            namedType.TypeKind != TypeKind.Class ||
            !namedType.Locations.Any(static location => location.IsInSource))
        {
            return;
        }

        var threshold = AnalyzerOptionReader.GetThreshold(
            context.Options,
            AnalyzerOptionReader.GetDeclaringTree(namedType),
            AnalyzerOptionReader.ParameterThresholdKey,
            DefaultParameterThreshold);
        foreach (var constructor in namedType.InstanceConstructors.Where(constructor => !constructor.IsImplicitlyDeclared && constructor.Parameters.Length > threshold))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                constructor.Locations[0],
                namedType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                constructor.Parameters.Length,
                threshold));
        }
    }
}
