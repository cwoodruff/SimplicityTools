using System.Globalization;
using SimplicityTools.Filters;
using SimplicityTools.Metrics;
using SimplicityTools.Tca;
using Xunit;

namespace SimplicityTools.Tca.Tests;

public sealed class TcaEstimateTests
{
    [Fact]
    public void Create_ComputesExcessOverTargetCostRangesForAllCategories()
    {
        var estimate = TcaEstimate.Create(CreateSnapshot(), CreateFilterVerdicts(), TcaInputs.Defaults);

        // Infrastructure: baseline 6 * $200 * 12 = $14,400; excess factor 4 unused deps * 0.05 = 0.2.
        Assert.Equal(new MoneyRange(2_016m, 3_744m), estimate.InfrastructureCostPerYear);
        // Operational: excess factor (7.5 - 5) / 5 = 0.5; 0.5 * $150 * 4 incidents * 12 * 4 = $14,400.
        Assert.Equal(new MoneyRange(10_080m, 18_720m), estimate.OperationalCostPerYear);
        // Coordination: excess factor (6 - 3) / 3 = 1.0; 1.0 * 3 * $4,000 * 12 = $144,000.
        Assert.Equal(new MoneyRange(100_800m, 187_200m), estimate.CoordinationCostPerYear);
        // Cognitive: excess factor (60 - 40) / 40 = 0.5; uplift 1 + (0.25 * 0.5) = 1.125 => $121,500.
        Assert.Equal(new MoneyRange(85_050m, 157_950m), estimate.CognitiveCostPerYear);
        // Opportunity: composite score 0.5; 8 * $15,000 * 12 * 0.5 * 0.4 = $288,000.
        Assert.Equal(new MoneyRange(201_600m, 374_400m), estimate.OpportunityCostPerYear);
    }

    [Fact]
    public void TotalPerYear_SumsAllCostCategories()
    {
        var estimate = TcaEstimate.Create(CreateSnapshot(), CreateFilterVerdicts(), TcaInputs.Defaults);

        Assert.Equal(new MoneyRange(399_546m, 742_014m), estimate.TotalPerYear);
    }

    [Fact]
    public void Create_ReportsBaselineOperatingCostSeparately()
    {
        var estimate = TcaEstimate.Create(CreateSnapshot(), CreateFilterVerdicts(), TcaInputs.Defaults);

        // Baseline: infrastructure 6 * $200 * 12 = $14,400 plus coordination min(6, 3) * $4,000 * 12 = $144,000.
        Assert.Equal(158_400m, estimate.BaselineOperatingCostPerYear);
    }

    [Fact]
    public void Create_AtTargetSnapshot_ProducesZeroArchitectureCostPerDimension()
    {
        var estimate = TcaEstimate.Create(CreateAtTargetSnapshot(), CreateAtTargetVerdicts(), TcaInputs.Defaults);

        Assert.Equal(new MoneyRange(0m, 0m), estimate.InfrastructureCostPerYear);
        Assert.Equal(new MoneyRange(0m, 0m), estimate.OperationalCostPerYear);
        Assert.Equal(new MoneyRange(0m, 0m), estimate.CoordinationCostPerYear);
        Assert.Equal(new MoneyRange(0m, 0m), estimate.CognitiveCostPerYear);
        Assert.Equal(new MoneyRange(0m, 0m), estimate.OpportunityCostPerYear);
        Assert.Equal(new MoneyRange(0m, 0m), estimate.TotalPerYear);

        // Baseline operating cost is still reported: 3 * $200 * 12 + 3 * $4,000 * 12.
        Assert.Equal(151_200m, estimate.BaselineOperatingCostPerYear);
    }

    [Fact]
    public void Create_OverTargetMetrics_ScaleWithExcessOnly()
    {
        // Complexity 10 is 100% over the target of 5, so operational cost doubles vs. 50% over.
        var halfOver = CreateSnapshot() with { AverageMethodComplexity = 7.5 };
        var fullOver = CreateSnapshot() with { AverageMethodComplexity = 10.0 };

        var halfOverEstimate = TcaEstimate.Create(halfOver, CreateFilterVerdicts(), TcaInputs.Defaults);
        var fullOverEstimate = TcaEstimate.Create(fullOver, CreateFilterVerdicts(), TcaInputs.Defaults);

        Assert.Equal(halfOverEstimate.OperationalCostPerYear.Low * 2m, fullOverEstimate.OperationalCostPerYear.Low);
        Assert.Equal(halfOverEstimate.OperationalCostPerYear.High * 2m, fullOverEstimate.OperationalCostPerYear.High);
    }

    [Fact]
    public void Create_CapsEveryExcessFactorAtMaxExcessFactor()
    {
        var snapshot = CreateSnapshot() with
        {
            AverageMethodComplexity = 50.0,
            TotalProjects = 60,
            UnusedDependencyCount = 200,
            EstimatedOnboardingTime = TimeSpan.FromHours(4_000)
        };

        var estimate = TcaEstimate.Create(snapshot, CreateFilterVerdicts(), TcaInputs.Defaults);

        // Operational: capped excess factor 3.0 * $150 * 4 * 12 * 4 = $86,400.
        Assert.Equal(new MoneyRange(60_480m, 112_320m), estimate.OperationalCostPerYear);
        // Infrastructure: 200 unused deps * 0.05 = 10.0, capped at 3.0 over baseline 60 * $200 * 12 = $144,000.
        Assert.Equal(new MoneyRange(302_400m, 561_600m), estimate.InfrastructureCostPerYear);
        // Coordination: (60 - 3) / 3 = 19.0, capped at 3.0 * 3 * $4,000 * 12 = $432,000.
        Assert.Equal(new MoneyRange(302_400m, 561_600m), estimate.CoordinationCostPerYear);
        // Cognitive: capped excess factor 3.0 * $15,000 * 12 * 0.15 * 8 * 1.125 = $729,000.
        Assert.Equal(new MoneyRange(510_300m, 947_700m), estimate.CognitiveCostPerYear);
    }

    [Fact]
    public void Create_NullOnboardingTime_TreatsCognitiveAsZeroExcessAndNotesItIsNotMeasured()
    {
        var snapshot = CreateSnapshot() with { EstimatedOnboardingTime = null };

        var estimate = TcaEstimate.Create(snapshot, CreateFilterVerdicts(), TcaInputs.Defaults);

        Assert.Equal(new MoneyRange(0m, 0m), estimate.CognitiveCostPerYear);
        Assert.Contains(estimate.Notes, static note => note.Contains("not measured", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("not measured", estimate.ToExecutiveSummary(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Create_NonFiniteComplexity_TreatsOperationalAsZeroExcessAndNotesIt(double complexity)
    {
        var snapshot = CreateSnapshot() with { AverageMethodComplexity = complexity };

        var estimate = TcaEstimate.Create(snapshot, CreateFilterVerdicts(), TcaInputs.Defaults);

        Assert.Equal(new MoneyRange(0m, 0m), estimate.OperationalCostPerYear);
        Assert.Contains(estimate.Notes, static note => note.Contains("finite", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Create_NonFiniteFilterScore_TreatsOpportunityScoreAsZeroExcessAndNotesIt()
    {
        var verdicts = new[]
        {
            CreateVerdict(FilterName.TwoAmTest, double.NaN),
            CreateVerdict(FilterName.HalfRule, 1.0),
            CreateVerdict(FilterName.PrimaryPathFirst, 1.0)
        };

        var estimate = TcaEstimate.Create(CreateSnapshot(), verdicts, TcaInputs.Defaults);

        Assert.Equal(new MoneyRange(0m, 0m), estimate.OpportunityCostPerYear);
        Assert.Contains(estimate.Notes, static note => note.Contains("finite", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Create_NegativeMetricsAndInputs_NeverProduceNegativeDollars()
    {
        var snapshot = CreateSnapshot() with
        {
            TotalProjects = -4,
            UnusedDependencyCount = -10,
            AverageMethodComplexity = -3.0
        };
        var inputs = TcaInputs.Defaults with
        {
            TeamSize = -8,
            AverageEngineerMonthlySalaryUsd = -15_000m,
            EstimatedMonthlyIncidentCount = -4,
            OnCallHourlyRateUsd = -150m,
            AttritionCoefficientPercent = -15m
        };

        var estimate = TcaEstimate.Create(snapshot, CreateFilterVerdicts(), inputs);

        foreach (var range in new[]
                 {
                     estimate.InfrastructureCostPerYear,
                     estimate.OperationalCostPerYear,
                     estimate.CoordinationCostPerYear,
                     estimate.CognitiveCostPerYear,
                     estimate.OpportunityCostPerYear,
                     estimate.TotalPerYear
                 })
        {
            Assert.True(range.Low >= 0m, $"Low bound {range.Low} must not be negative.");
            Assert.True(range.High >= 0m, $"High bound {range.High} must not be negative.");
        }

        Assert.True(estimate.BaselineOperatingCostPerYear >= 0m);
    }

    [Fact]
    public void Create_DuplicateEquivalentVerdicts_UsesFirstWithoutThrowing()
    {
        var verdicts = new List<FilterVerdict>(CreateFilterVerdicts())
        {
            CreateVerdict(FilterName.TwoAmTest, 0.50)
        };

        var estimate = TcaEstimate.Create(CreateSnapshot(), verdicts, TcaInputs.Defaults);

        Assert.Equal(new MoneyRange(201_600m, 374_400m), estimate.OpportunityCostPerYear);
    }

    [Fact]
    public void Create_ConflictingDuplicateVerdicts_ThrowsClearError()
    {
        var verdicts = new List<FilterVerdict>(CreateFilterVerdicts())
        {
            CreateVerdict(FilterName.TwoAmTest, 0.90)
        };

        var exception = Assert.Throws<ArgumentException>(
            () => TcaEstimate.Create(CreateSnapshot(), verdicts, TcaInputs.Defaults));

        Assert.Contains(nameof(FilterName.TwoAmTest), exception.Message, StringComparison.Ordinal);
        Assert.Contains("onflicting", exception.Message, StringComparison.Ordinal);
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
    public void Defaults_ExposeDocumentedModelConstants()
    {
        var defaults = TcaInputs.Defaults;

        Assert.Equal(200m, defaults.MonthlyInfrastructureCostPerProjectUsd);
        Assert.Equal(0.05m, defaults.InfrastructureExcessPerUnusedDependency);
        Assert.Equal(4m, defaults.IncidentCostMultiplier);
        Assert.Equal(4_000m, defaults.MonthlyCoordinationCostPerProjectUsd);
        Assert.Equal(3, defaults.BaselineProjectCount);
        Assert.Equal(0.5m, defaults.PrematureAbstractionUpliftFactor);
        Assert.Equal(0.4m, defaults.PayrollOpportunityFactor);
        Assert.Equal(0.7m, defaults.RangeLowMultiplier);
        Assert.Equal(1.3m, defaults.RangeHighMultiplier);
        Assert.Equal(3.0m, defaults.MaxExcessFactor);
        Assert.Equal(5m, defaults.TargetAverageMethodComplexity);
        Assert.Equal(40m, defaults.TargetOnboardingHours);
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
                "Architecture excess over simplicity targets:",
                "Infrastructure:   $2,016 - $3,744",
                "Operational:      $10,080 - $18,720",
                "Coordination:     $100,800 - $187,200",
                "Cognitive:        $85,050 - $157,950",
                "Opportunity:      $201,600 - $374,400",
                "--------------------------------------------",
                "TOTAL EXCESS:     $399,546 - $742,014 per year",
                "Baseline operating cost at target: $158,400 per year (not attributed to architecture)"
            ]);

        Assert.Equal(expected, estimate.ToExecutiveSummary());
    }

    [Fact]
    public void ToExecutiveSummary_AppendsNotesWhenPresent()
    {
        var snapshot = CreateSnapshot() with { EstimatedOnboardingTime = null };

        var estimate = TcaEstimate.Create(snapshot, CreateFilterVerdicts(), TcaInputs.Defaults);
        var summary = estimate.ToExecutiveSummary();

        Assert.Contains($"{Environment.NewLine}Note: ", summary, StringComparison.Ordinal);
    }

    private static SimplicitySnapshot CreateSnapshot() =>
        new()
        {
            TotalProjects = 6,
            TotalFiles = 100,
            PrimaryPathFileCount = 40,
            AbstractionLayerCount = 12,
            ExternalDependencyCount = 20,
            UnusedDependencyCount = 4,
            InterfacesWithSingleImplementation = 3,
            AverageMethodComplexity = 7.5,
            EstimatedOnboardingTime = TimeSpan.FromHours(60),
            CollectedAt = DateTimeOffset.Parse("2026-04-29T21:22:50.867-04:00")
        };

    private static SimplicitySnapshot CreateAtTargetSnapshot() =>
        new()
        {
            TotalProjects = 3,
            TotalFiles = 100,
            PrimaryPathFileCount = 60,
            AbstractionLayerCount = 4,
            ExternalDependencyCount = 10,
            UnusedDependencyCount = 0,
            InterfacesWithSingleImplementation = 0,
            AverageMethodComplexity = 5.0,
            EstimatedOnboardingTime = TimeSpan.FromHours(40),
            CollectedAt = DateTimeOffset.Parse("2026-04-29T21:22:50.867-04:00")
        };

    private static FilterVerdict[] CreateFilterVerdicts() =>
        [
            CreateVerdict(FilterName.TwoAmTest, 0.50),
            CreateVerdict(FilterName.HalfRule, 0.75),
            CreateVerdict(FilterName.PrimaryPathFirst, 0.25)
        ];

    private static FilterVerdict[] CreateAtTargetVerdicts() =>
        [
            CreateVerdict(FilterName.TwoAmTest, 1.0),
            CreateVerdict(FilterName.HalfRule, 1.0),
            CreateVerdict(FilterName.PrimaryPathFirst, 1.0)
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
