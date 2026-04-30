using System.Globalization;
using SimplicityTools.Filters;
using SimplicityTools.Metrics;
using SimplicityTools.Tca;
using Xunit;

namespace SimplicityTools.Tca.Tests;

public sealed class TcaEstimateTests
{
    [Fact]
    public void Create_ComputesAnnualCostRangesForAllCategories()
    {
        var estimate = TcaEstimate.Create(CreateSnapshot(), CreateFilterVerdicts(), TcaInputs.Defaults);

        Assert.Equal(new MoneyRange(13_824m, 20_736m), estimate.InfrastructureCostPerYear);
        Assert.Equal(new MoneyRange(30_240m, 56_160m), estimate.OperationalCostPerYear);
        Assert.Equal(new MoneyRange(108_000m, 180_000m), estimate.CoordinationCostPerYear);
        Assert.Equal(new MoneyRange(255_150m, 473_850m), estimate.CognitiveCostPerYear);
        Assert.Equal(new MoneyRange(144_000m, 432_000m), estimate.OpportunityCostPerYear);
    }

    [Fact]
    public void TotalPerYear_SumsAllCostCategories()
    {
        var estimate = TcaEstimate.Create(CreateSnapshot(), CreateFilterVerdicts(), TcaInputs.Defaults);

        Assert.Equal(new MoneyRange(551_214m, 1_162_746m), estimate.TotalPerYear);
    }

    [Fact]
    public void Create_ThrowsWhenARequiredFilterVerdictIsMissing()
    {
        var verdicts = CreateFilterVerdicts()
            .Where(static verdict => verdict.Filter != FilterName.PrimaryPathFirst);

        var exception = Assert.Throws<ArgumentException>(() => TcaEstimate.Create(CreateSnapshot(), verdicts, TcaInputs.Defaults));

        Assert.Contains(nameof(FilterName.PrimaryPathFirst), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ToExecutiveSummary_UsesSpecifiedFormat_IndependentlyOfCurrentCulture()
    {
        using var _ = new CultureScope("fr-FR");

        var estimate = TcaEstimate.Create(CreateSnapshot(), CreateFilterVerdicts(), TcaInputs.Defaults);

        var expected = string.Join(
            Environment.NewLine,
            [
                "Total Cost of Architecture (Annual Estimate)",
                "============================================",
                "Infrastructure:   $13,824 - $20,736",
                "Operational:      $30,240 - $56,160",
                "Coordination:     $108,000 - $180,000",
                "Cognitive:        $255,150 - $473,850",
                "Opportunity:      $144,000 - $432,000",
                "--------------------------------------------",
                "TOTAL:            $551,214 - $1,162,746 per year"
            ]);

        Assert.Equal(expected, estimate.ToExecutiveSummary());
    }

    private static SimplicitySnapshot CreateSnapshot() =>
        new(
            TotalProjects: 6,
            TotalFiles: 100,
            PrimaryPathFileCount: 40,
            AbstractionLayerCount: 12,
            ExternalDependencyCount: 20,
            UnusedDependencyCount: 4,
            InterfacesWithSingleImplementation: 3,
            AverageMethodComplexity: 7.5,
            EstimatedOnboardingTime: TimeSpan.FromHours(60),
            CollectedAt: DateTimeOffset.Parse("2026-04-29T21:22:50.867-04:00"));

    private static FilterVerdict[] CreateFilterVerdicts() =>
        [
            CreateVerdict(FilterName.TwoAmTest, 0.50),
            CreateVerdict(FilterName.HalfRule, 0.75),
            CreateVerdict(FilterName.PrimaryPathFirst, 0.25)
        ];

    private static FilterVerdict CreateVerdict(FilterName filter, double score) =>
        new(
            filter,
            Passes: score >= 0.70,
            Score: score,
            Summary: $"{filter} summary",
            SubScores: [],
            Violations: [],
            Recommendations: []);

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo originalCulture;
        private readonly CultureInfo originalUICulture;

        public CultureScope(string cultureName)
        {
            originalCulture = CultureInfo.CurrentCulture;
            originalUICulture = CultureInfo.CurrentUICulture;

            var culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }
}
