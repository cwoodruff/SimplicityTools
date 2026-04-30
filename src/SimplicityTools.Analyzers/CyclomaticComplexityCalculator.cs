using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SimplicityTools.Analyzers;

internal static class CyclomaticComplexityCalculator
{
    public static bool TryCalculate(SyntaxNode declaration, out int complexity)
    {
        var executableRoot = declaration switch
        {
            MethodDeclarationSyntax method => method.Body is not null ? (SyntaxNode)method.Body : method.ExpressionBody?.Expression,
            LocalFunctionStatementSyntax localFunction => localFunction.Body is not null ? (SyntaxNode)localFunction.Body : localFunction.ExpressionBody?.Expression,
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
