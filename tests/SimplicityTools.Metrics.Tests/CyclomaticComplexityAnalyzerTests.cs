using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SimplicityTools.Metrics;
using Xunit;

namespace SimplicityTools.Metrics.Tests;

public sealed class CyclomaticComplexityAnalyzerTests
{
    [Theory]
    [InlineData("void M() { }", 1)]
    [InlineData("void M(bool flag) { if (flag) { } }", 2)]
    [InlineData("void M(bool flag) { if (flag) { } else { } }", 2)]
    [InlineData("void M(bool left, bool right) { if (left) { if (right) { } } }", 3)]
    [InlineData("void M(bool left, bool right) { _ = left && right; }", 2)]
    [InlineData("void M(bool left, bool right) { _ = left || right; }", 2)]
    [InlineData("int M(bool flag) => flag ? 1 : 0;", 2)]
    [InlineData("int M(int value) { return value switch { 1 => 1, 2 => 2, _ => 0 }; }", 3)]
    [InlineData("string M(string? value) { return value ?? \"fallback\"; }", 2)]
    [InlineData("string M(Node? node) { return node?.Name ?? \"missing\"; }", 3)]
    [InlineData("void M(ref string? value) { value ??= \"fallback\"; }", 2)]
    [InlineData("bool M(int value) => value is > 0 and < 10;", 2)]
    [InlineData("bool M(object value) => value is int or string;", 2)]
    [InlineData("int M(object value) { switch (value) { case int number: return number; default: return 0; } }", 2)]
    public void Calculate_FollowsDocumentedCountingRules(string memberDeclaration, int expectedComplexity)
    {
        var declaration = ParseMethod(memberDeclaration);

        var complexity = CyclomaticComplexityAnalyzer.Calculate(declaration);

        Assert.Equal(expectedComplexity, complexity);
    }

    private static MethodDeclarationSyntax ParseMethod(string memberDeclaration)
    {
        var compilationUnit = CSharpSyntaxTree.ParseText(
            $$"""
            namespace TestHarness;

            public sealed class Node
            {
                public string Name { get; init; } = string.Empty;
            }

            public sealed class Sample
            {
                {{memberDeclaration}}
            }
            """
            ).GetRoot();

        return compilationUnit.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
    }
}
