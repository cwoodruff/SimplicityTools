using Microsoft.CodeAnalysis.CSharp;
using SimplicityTools.Tests.Shared;
using Xunit;
using static SimplicityTools.Analyzers.Tests.AnalyzerTestInfrastructure;

namespace SimplicityTools.Analyzers.Tests;

/// <summary>
/// Runs the shared modern-C# complexity battery against the analyzer-side counter. The same
/// battery runs against the Metrics-side counter in SimplicityTools.Metrics.Tests, proving both
/// implementations produce identical numbers.
/// </summary>
public sealed class ComplexityCountingTests
{
    [Theory]
    [MemberData(nameof(ComplexityCountingTestCases.Cases), MemberType = typeof(ComplexityCountingTestCases))]
    public void TryCalculate_MatchesDocumentedCountingRules(string source, int[] expectedComplexities)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var complexities = new List<int>();
        foreach (var node in root.DescendantNodesAndSelf())
        {
            if (CyclomaticComplexityCalculator.TryCalculate(node, out var complexity))
            {
                complexities.Add(complexity);
            }
        }

        Assert.Equal(expectedComplexities, complexities);
    }

    [Fact]
    public async Task HighComplexityAnalyzer_ReportsLocalFunctionSeparatelyFromParent()
    {
        var diagnostics = await RunAsync(
            new HighComplexityAnalyzer(),
            [
                new SourceFile("/repo/LocalFunctions.cs", """
                namespace Demo;

                public sealed class Feature
                {
                    public int Outer(int value)
                    {
                        return Classify(value);

                        int Classify(int candidate)
                        {
                            if (candidate > 100) return 3;
                            if (candidate > 10) return 2;
                            if (candidate > 0) return 1;
                            return 0;
                        }
                    }
                }
                """)
            ],
            globalOptions: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["simplicity_first.sf0003_complexity_threshold"] = "2"
            });

        // Outer has complexity 1 (the local function body is excluded); Classify has 4.
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("Local function 'Classify'", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("complexity 4", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HighComplexityAnalyzer_CountsLambdaTowardContainingMethod()
    {
        var diagnostics = await RunAsync(
            new HighComplexityAnalyzer(),
            [
                new SourceFile("/repo/Lambdas.cs", """
                namespace Demo;

                public sealed class Feature
                {
                    public Func<int, int> Build(bool flag)
                    {
                        if (flag)
                        {
                            return value => value > 0 ? value : -value;
                        }

                        return value => value;
                    }
                }
                """)
            ],
            globalOptions: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["simplicity_first.sf0003_complexity_threshold"] = "2"
            });

        // Build: 1 + if + conditional inside the lambda = 3.
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("Method 'Build'", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("complexity 3", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HighComplexityAnalyzer_ReportsAccessorBody()
    {
        var diagnostics = await RunAsync(
            new HighComplexityAnalyzer(),
            [
                new SourceFile("/repo/Accessors.cs", """
                namespace Demo;

                public sealed class Feature
                {
                    private int _value;

                    public int Value
                    {
                        get => _value;
                        set
                        {
                            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
                            if (value > 100) throw new ArgumentOutOfRangeException(nameof(value));
                            _value = value;
                        }
                    }
                }
                """)
            ],
            globalOptions: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["simplicity_first.sf0003_complexity_threshold"] = "2"
            });

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("Accessor 'set' of 'Value'", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("complexity 3", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HighComplexityAnalyzer_ReportsTopLevelStatements()
    {
        var diagnostics = await RunAsync(
            new HighComplexityAnalyzer(),
            [
                new SourceFile("/repo/Program.cs", """
                var arguments = Environment.GetCommandLineArgs();
                if (arguments.Length > 1 && arguments[1] == "verbose")
                {
                    Console.WriteLine("verbose");
                }

                foreach (var argument in arguments)
                {
                    Console.WriteLine(argument);
                }
                """)
            ],
            globalOptions: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["simplicity_first.sf0003_complexity_threshold"] = "2"
            });

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("Top-level statements", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("complexity 4", diagnostic.GetMessage(), StringComparison.Ordinal);
    }
}
