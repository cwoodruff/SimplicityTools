using System.Globalization;
using SimplicityTools.Filters;
using SimplicityTools.Metrics;

namespace SimplicityTools.Tca;

public sealed record TcaEstimate(
    MoneyRange InfrastructureCostPerYear,
    MoneyRange OperationalCostPerYear,
    MoneyRange CoordinationCostPerYear,
    MoneyRange CognitiveCostPerYear,
    MoneyRange OpportunityCostPerYear)
{
    public MoneyRange TotalPerYear =>
        InfrastructureCostPerYear + OperationalCostPerYear +
        CoordinationCostPerYear + CognitiveCostPerYear +
        OpportunityCostPerYear;

    public string ToExecutiveSummary() =>
        string.Join(
            Environment.NewLine,
            [
                "Total Cost of Architecture (Annual Estimate)",
                "============================================",
                $"Infrastructure:   {InfrastructureCostPerYear}",
                $"Operational:      {OperationalCostPerYear}",
                $"Coordination:     {CoordinationCostPerYear}",
                $"Cognitive:        {CognitiveCostPerYear}",
                $"Opportunity:      {OpportunityCostPerYear}",
                "--------------------------------------------",
                $"TOTAL:            {TotalPerYear} per year"
            ]);

    public static TcaEstimate Create(
        SimplicitySnapshot snapshot,
        IEnumerable<FilterVerdict> filterVerdicts) =>
        Create(snapshot, filterVerdicts, TcaInputs.Defaults);

    public static TcaEstimate Create(
        SimplicitySnapshot snapshot,
        IEnumerable<FilterVerdict> filterVerdicts,
        TcaInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(filterVerdicts);
        ArgumentNullException.ThrowIfNull(inputs);

        var verdictsByFilter = filterVerdicts.ToDictionary(static verdict => verdict.Filter);
        var twoAmVerdict = GetRequiredVerdict(verdictsByFilter, FilterName.TwoAmTest);
        var halfRuleVerdict = GetRequiredVerdict(verdictsByFilter, FilterName.HalfRule);
        var primaryPathVerdict = GetRequiredVerdict(verdictsByFilter, FilterName.PrimaryPathFirst);

        var infrastructureAnnual = snapshot.TotalProjects
            * 200m
            * 12m
            * Math.Min(2.0m, 1.0m + (snapshot.UnusedDependencyCount * 0.05m));

        var operationalAnnual = ((decimal)snapshot.AverageMethodComplexity / 5m)
            * inputs.OnCallHourlyRateUsd
            * inputs.EstimatedMonthlyIncidentCount
            * 12m
            * 4m;

        var coordinationAnnual = Math.Max(0, snapshot.TotalProjects - 3) * 4000m * 12m;

        var cognitiveAnnual = ((decimal)snapshot.EstimatedOnboardingTime.TotalHours / 40m)
            * inputs.AverageEngineerMonthlySalaryUsd
            * 12m
            * (inputs.AttritionCoefficientPercent / 100m)
            * inputs.TeamSize
            * (1.0m + ((decimal)snapshot.PrematureAbstractionRatio * 0.5m));

        var compositeScore = (
            ClampScore(twoAmVerdict.Score) +
            ClampScore(halfRuleVerdict.Score) +
            ClampScore(primaryPathVerdict.Score)) / 3m;

        var opportunityAnnual = inputs.TeamSize
            * inputs.AverageEngineerMonthlySalaryUsd
            * 12m
            * (1.0m - compositeScore)
            * 0.4m;

        return new TcaEstimate(
            CreateRange(infrastructureAnnual, 0.8m, 1.2m),
            CreateRange(operationalAnnual, 0.7m, 1.3m),
            CreateRange(coordinationAnnual, 0.75m, 1.25m),
            CreateRange(cognitiveAnnual, 0.7m, 1.3m),
            CreateRange(opportunityAnnual, 0.5m, 1.5m));
    }

    private static FilterVerdict GetRequiredVerdict(
        IReadOnlyDictionary<FilterName, FilterVerdict> verdictsByFilter,
        FilterName filter)
    {
        if (verdictsByFilter.TryGetValue(filter, out var verdict))
        {
            return verdict;
        }

        throw new ArgumentException($"A verdict for {filter} is required to calculate TCA.", nameof(verdictsByFilter));
    }

    private static decimal ClampScore(double score)
        => Math.Clamp((decimal)score, 0m, 1m);

    private static MoneyRange CreateRange(decimal annualCost, decimal lowMultiplier, decimal highMultiplier)
        => new(annualCost * lowMultiplier, annualCost * highMultiplier);
}

public sealed record TcaInputs(
    int TeamSize,
    decimal AverageEngineerMonthlySalaryUsd,
    int EstimatedMonthlyIncidentCount,
    decimal OnCallHourlyRateUsd,
    decimal AttritionCoefficientPercent)
{
    public static TcaInputs Defaults { get; } = new(
        TeamSize: 8,
        AverageEngineerMonthlySalaryUsd: 15000m,
        EstimatedMonthlyIncidentCount: 4,
        OnCallHourlyRateUsd: 150m,
        AttritionCoefficientPercent: 15m);
}

public readonly record struct MoneyRange(decimal Low, decimal High)
{
    public static MoneyRange operator +(MoneyRange a, MoneyRange b) =>
        new(a.Low + b.Low, a.High + b.High);

    public override string ToString() =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"${Low:N0} - ${High:N0}");
}
