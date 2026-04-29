using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SimplicityTools.Metrics;

internal static class CyclomaticComplexityAnalyzer
{
    public static IReadOnlyList<int> Collect(SyntaxNode root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var complexities = new List<int>();
        foreach (var declaration in root.DescendantNodes().Where(IsAnalyzableDeclaration))
        {
            if (TryCalculate(declaration, out var complexity))
            {
                complexities.Add(complexity);
            }
        }

        return complexities;
    }

    public static int Calculate(SyntaxNode declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);

        return TryCalculate(declaration, out var complexity)
            ? complexity
            : throw new ArgumentException("The supplied syntax node does not represent an executable member body.", nameof(declaration));
    }

    private static bool IsAnalyzableDeclaration(SyntaxNode node)
    {
        return node is MethodDeclarationSyntax or
            ConstructorDeclarationSyntax or
            AccessorDeclarationSyntax or
            LocalFunctionStatementSyntax or
            OperatorDeclarationSyntax or
            ConversionOperatorDeclarationSyntax or
            PropertyDeclarationSyntax or
            IndexerDeclarationSyntax;
    }

    private static bool TryCalculate(SyntaxNode declaration, out int complexity)
    {
        var executableRoot = declaration switch
        {
            MethodDeclarationSyntax method => method.Body is not null ? (SyntaxNode)method.Body : method.ExpressionBody?.Expression,
            ConstructorDeclarationSyntax constructor => constructor.Body is not null ? (SyntaxNode)constructor.Body : constructor.ExpressionBody?.Expression,
            AccessorDeclarationSyntax accessor => accessor.Body is not null ? (SyntaxNode)accessor.Body : accessor.ExpressionBody?.Expression,
            LocalFunctionStatementSyntax localFunction => localFunction.Body is not null ? (SyntaxNode)localFunction.Body : localFunction.ExpressionBody?.Expression,
            OperatorDeclarationSyntax operatorDeclaration => operatorDeclaration.Body is not null ? (SyntaxNode)operatorDeclaration.Body : operatorDeclaration.ExpressionBody?.Expression,
            ConversionOperatorDeclarationSyntax conversionOperator => conversionOperator.Body is not null ? (SyntaxNode)conversionOperator.Body : conversionOperator.ExpressionBody?.Expression,
            PropertyDeclarationSyntax property when property.AccessorList is null => (SyntaxNode?)property.ExpressionBody?.Expression,
            IndexerDeclarationSyntax indexer when indexer.AccessorList is null => (SyntaxNode?)indexer.ExpressionBody?.Expression,
            _ => null
        };

        if (executableRoot is null)
        {
            complexity = 0;
            return false;
        }

        complexity = 1;
        foreach (var node in executableRoot.DescendantNodesAndSelf())
        {
            switch (node)
            {
                case IfStatementSyntax:
                case ForStatementSyntax:
                case ForEachStatementSyntax:
                case ForEachVariableStatementSyntax:
                case WhileStatementSyntax:
                case DoStatementSyntax:
                case CatchClauseSyntax:
                case ConditionalExpressionSyntax:
                case ConditionalAccessExpressionSyntax:
                case SwitchExpressionArmSyntax:
                    complexity++;
                    break;
                case SwitchSectionSyntax switchSection:
                    complexity += switchSection.Labels.OfType<CaseSwitchLabelSyntax>().Count();
                    break;
                case BinaryExpressionSyntax binaryExpression when binaryExpression.IsKind(SyntaxKind.LogicalAndExpression) ||
                                                                  binaryExpression.IsKind(SyntaxKind.LogicalOrExpression) ||
                                                                  binaryExpression.IsKind(SyntaxKind.CoalesceExpression):
                    complexity++;
                    break;
            }
        }

        return true;
    }
}
