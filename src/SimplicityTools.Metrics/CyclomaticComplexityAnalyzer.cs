using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SimplicityTools.Metrics;

/// <summary>
/// Counts cyclomatic complexity per measured unit. The counting rules are documented in
/// docs/using-the-simplicity-tools.md ("Complexity counting rules") and MUST stay in sync with
/// <c>SimplicityTools.Analyzers.CyclomaticComplexityCalculator</c>, which duplicates this logic
/// because analyzer assemblies cannot reference this one.
///
/// Rules summary:
/// - Measured units: methods, constructors, operators, conversion operators, accessor bodies,
///   expression-bodied properties/indexers, local functions, and a file's top-level statement
///   block. Each unit starts at 1.
/// - +1 per: if, for, foreach, while, do, catch clause, conditional expression (?:), conditional
///   access (?.), case label (constant or pattern; "default:" is free), switch expression arm
///   (the discard arm "_ =>" without a when clause is free), &amp;&amp;, ||, ??, ??=, and each
///   pattern "and"/"or" combinator.
/// - Local functions are measured as their own units and are excluded from the enclosing unit's
///   count. Lambdas and anonymous methods count toward the enclosing unit.
/// </summary>
internal static class CyclomaticComplexityAnalyzer
{
    public static IReadOnlyList<int> Collect(SyntaxNode root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var complexities = new List<int>();
        foreach (var declaration in root.DescendantNodesAndSelf().Where(IsAnalyzableDeclaration))
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
        return node is CompilationUnitSyntax or
            MethodDeclarationSyntax or
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
        if (declaration is CompilationUnitSyntax compilationUnit)
        {
            return TryCalculateTopLevelStatements(compilationUnit, out complexity);
        }

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

        complexity = 1 + CountBranchNodes(executableRoot);
        return true;
    }

    /// <summary>
    /// Treats a file's top-level statements as one method-equivalent unit. Top-level local
    /// functions are their own units (collected separately), so they contribute nothing here.
    /// </summary>
    private static bool TryCalculateTopLevelStatements(CompilationUnitSyntax compilationUnit, out int complexity)
    {
        complexity = 0;
        var hasTopLevelStatements = false;
        foreach (var member in compilationUnit.Members)
        {
            if (member is not GlobalStatementSyntax globalStatement)
            {
                continue;
            }

            hasTopLevelStatements = true;
            if (globalStatement.Statement is not LocalFunctionStatementSyntax)
            {
                complexity += CountBranchNodes(globalStatement.Statement);
            }
        }

        if (!hasTopLevelStatements)
        {
            return false;
        }

        complexity += 1;
        return true;
    }

    private static int CountBranchNodes(SyntaxNode executableRoot)
    {
        var count = 0;

        // Do not descend into local functions: they are measured as separate units.
        foreach (var node in executableRoot.DescendantNodesAndSelf(static node => node is not LocalFunctionStatementSyntax))
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
                case CaseSwitchLabelSyntax:
                case CasePatternSwitchLabelSyntax:
                case BinaryPatternSyntax:
                    count++;
                    break;
                case SwitchExpressionArmSyntax arm when arm.Pattern is not DiscardPatternSyntax || arm.WhenClause is not null:
                    count++;
                    break;
                case BinaryExpressionSyntax binaryExpression when binaryExpression.IsKind(SyntaxKind.LogicalAndExpression) ||
                                                                  binaryExpression.IsKind(SyntaxKind.LogicalOrExpression) ||
                                                                  binaryExpression.IsKind(SyntaxKind.CoalesceExpression):
                case AssignmentExpressionSyntax assignment when assignment.IsKind(SyntaxKind.CoalesceAssignmentExpression):
                    count++;
                    break;
            }
        }

        return count;
    }
}
