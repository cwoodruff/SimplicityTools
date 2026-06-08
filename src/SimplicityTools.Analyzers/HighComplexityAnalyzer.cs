using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SimplicityTools.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HighComplexityAnalyzer : DiagnosticAnalyzer
{
    private const int ComplexityThreshold = 10;

    public const string DiagnosticId = "SF0003";

    public static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Method is too complex for fast understanding",
        messageFormat: "Method {0} has cyclomatic complexity {1}, which exceeds the limit of 10",
        category: AnalyzerCategories.TwoAmTest,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://simplicitytools.dev/analyzers/sf0003/",
        description: "Methods that exceed the agreed cyclomatic complexity threshold are hard to reason about under pressure.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Analyze, SymbolKind.Method);
    }

    private static void Analyze(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol method ||
            method.MethodKind != MethodKind.Ordinary ||
            !method.Locations.Any(static location => location.IsInSource))
        {
            return;
        }

        foreach (var syntaxReference in method.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax(context.CancellationToken) is not MethodDeclarationSyntax declaration ||
                !CyclomaticComplexityCalculator.TryCalculate(declaration, out var complexity) ||
                complexity <= ComplexityThreshold)
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                declaration.Identifier.GetLocation(),
                method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                complexity));
            break;
        }
    }
}
