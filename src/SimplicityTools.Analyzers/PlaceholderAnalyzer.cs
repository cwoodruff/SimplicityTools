using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SimplicityTools.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PlaceholderAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ST0001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Analyzer scaffold placeholder",
        "SimplicityTools analyzer scaffolding is in place",
        "Architecture",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Placeholder diagnostic that keeps the analyzer package build-safe until real rules land.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
    }
}
