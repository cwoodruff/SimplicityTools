using Microsoft.CodeAnalysis.CSharp;
using SimplicityTools.Tests.Shared;
using Xunit;

namespace SimplicityTools.Metrics.Tests;

/// <summary>
/// Runs the shared modern-C# complexity battery against the Metrics-side counter. The same
/// battery runs against the analyzer-side counter in SimplicityTools.Analyzers.Tests, proving
/// both implementations produce identical numbers.
/// </summary>
public sealed class ComplexityCountingTests
{
    [Theory]
    [MemberData(nameof(ComplexityCountingTestCases.Cases), MemberType = typeof(ComplexityCountingTestCases))]
    public void Collect_MatchesDocumentedCountingRules(string source, int[] expectedComplexities)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var complexities = CyclomaticComplexityAnalyzer.Collect(root);

        Assert.Equal(expectedComplexities, complexities);
    }
}
