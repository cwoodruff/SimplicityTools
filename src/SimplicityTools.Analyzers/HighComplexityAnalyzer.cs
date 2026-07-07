using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SimplicityTools.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HighComplexityAnalyzer : DiagnosticAnalyzer
{
    internal const int DefaultComplexityThreshold = 10;

    public const string DiagnosticId = "SF0003";

    public static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Method is too complex for fast understanding",
        messageFormat: "{0} has cyclomatic complexity {1}, which exceeds the limit of {2}",
        category: AnalyzerCategories.TwoAmTest,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://simplicitytools.dev/analyzers/sf0003/",
        description: "Methods that exceed the agreed cyclomatic complexity threshold are hard to reason about under pressure.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // One registration per measured unit, mirroring
        // CyclomaticComplexityCalculator.TryCalculate. Lambdas are not separate units — their
        // branches count toward the enclosing unit — and local functions are separate units.
        context.RegisterSyntaxNodeAction(
            Analyze,
            SyntaxKind.CompilationUnit,
            SyntaxKind.MethodDeclaration,
            SyntaxKind.ConstructorDeclaration,
            SyntaxKind.OperatorDeclaration,
            SyntaxKind.ConversionOperatorDeclaration,
            SyntaxKind.PropertyDeclaration,
            SyntaxKind.IndexerDeclaration,
            SyntaxKind.GetAccessorDeclaration,
            SyntaxKind.SetAccessorDeclaration,
            SyntaxKind.InitAccessorDeclaration,
            SyntaxKind.AddAccessorDeclaration,
            SyntaxKind.RemoveAccessorDeclaration,
            SyntaxKind.LocalFunctionStatement);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        if (!CyclomaticComplexityCalculator.TryCalculate(context.Node, out var complexity))
        {
            return;
        }

        var threshold = AnalyzerOptionReader.GetThreshold(
            context.Options,
            context.Node.SyntaxTree,
            AnalyzerOptionReader.ComplexityThresholdKey,
            DefaultComplexityThreshold);
        if (complexity <= threshold)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            GetUnitLocation(context.Node),
            GetUnitDisplayName(context.Node),
            complexity,
            threshold));
    }

    private static string GetUnitDisplayName(SyntaxNode declaration)
    {
        return declaration switch
        {
            MethodDeclarationSyntax method => $"Method '{method.Identifier.Text}'",
            ConstructorDeclarationSyntax constructor => $"Constructor '{constructor.Identifier.Text}'",
            OperatorDeclarationSyntax operatorDeclaration => $"Operator '{operatorDeclaration.OperatorToken.Text}'",
            ConversionOperatorDeclarationSyntax conversionOperator => $"Conversion operator '{conversionOperator.Type}'",
            PropertyDeclarationSyntax property => $"Property '{property.Identifier.Text}'",
            IndexerDeclarationSyntax => "Indexer 'this[]'",
            AccessorDeclarationSyntax accessor => $"Accessor '{accessor.Keyword.Text}' of '{GetAccessorOwnerName(accessor)}'",
            LocalFunctionStatementSyntax localFunction => $"Local function '{localFunction.Identifier.Text}'",
            _ => "Top-level statements"
        };
    }

    private static string GetAccessorOwnerName(AccessorDeclarationSyntax accessor)
    {
        return accessor.FirstAncestorOrSelf<BasePropertyDeclarationSyntax>() switch
        {
            PropertyDeclarationSyntax property => property.Identifier.Text,
            EventDeclarationSyntax eventDeclaration => eventDeclaration.Identifier.Text,
            IndexerDeclarationSyntax => "this[]",
            _ => "?"
        };
    }

    private static Location GetUnitLocation(SyntaxNode declaration)
    {
        return declaration switch
        {
            MethodDeclarationSyntax method => method.Identifier.GetLocation(),
            ConstructorDeclarationSyntax constructor => constructor.Identifier.GetLocation(),
            OperatorDeclarationSyntax operatorDeclaration => operatorDeclaration.OperatorToken.GetLocation(),
            ConversionOperatorDeclarationSyntax conversionOperator => conversionOperator.Type.GetLocation(),
            PropertyDeclarationSyntax property => property.Identifier.GetLocation(),
            IndexerDeclarationSyntax indexer => indexer.ThisKeyword.GetLocation(),
            AccessorDeclarationSyntax accessor => accessor.Keyword.GetLocation(),
            LocalFunctionStatementSyntax localFunction => localFunction.Identifier.GetLocation(),
            CompilationUnitSyntax compilationUnit when compilationUnit.Members.OfType<GlobalStatementSyntax>().FirstOrDefault() is { } globalStatement =>
                globalStatement.GetFirstToken().GetLocation(),
            _ => declaration.GetFirstToken().GetLocation()
        };
    }
}
