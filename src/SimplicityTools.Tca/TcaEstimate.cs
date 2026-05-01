using System.Globalization;
using SimplicityTools.Filters;
using SimplicityTools.Metrics;

namespace SimplicityTools.Tca;

/// <summary>
/// Represents the annual Total Cost of Architecture estimate broken down by major cost category.
/// </summary>
/// <param name="InfrastructureCostPerYear">The estimated annual infrastructure cost range caused by excess complexity.</param>
/// <param name="OperationalCostPerYear">The estimated annual operational cost range caused by excess complexity.</param>
/// <param name="CoordinationCostPerYear">The estimated annual coordination cost range caused by excess complexity.</param>
/// <param name="CognitiveCostPerYear">The estimated annual cognitive load cost range caused by excess complexity.</param>
/// <param name="OpportunityCostPerYear">The estimated annual opportunity cost range caused by excess complexity.</param>
public sealed record TcaEstimate(
    MoneyRange InfrastructureCostPerYear,
    MoneyRange OperationalCostPerYear,
    MoneyRange CoordinationCostPerYear,
    MoneyRange CognitiveCostPerYear,
    MoneyRange OpportunityCostPerYear)
{
    /// <summary>
    /// Gets the combined annual range across all TCA categories.
    /// </summary>
    public MoneyRange TotalPerYear =>
        InfrastructureCostPerYear + OperationalCostPerYear +
        CoordinationCostPerYear + CognitiveCostPerYear +
        OpportunityCostPerYear;

    /// <summary>
    /// Formats the estimate as the multi-line executive summary used by the CLI and documentation samples.
    /// </summary>
    /// <returns>A multi-line summary of the annual estimate.</returns>
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

    /// <summary>
    /// Creates a TCA estimate with the default calculation inputs.
    /// </summary>
    /// <param name="snapshot">The measured solution snapshot.</param>
    /// <param name="filterVerdicts">The Simplicity-First filter verdicts that feed the opportunity-cost model.</param>
    /// <returns>The calculated annual estimate.</returns>
    public static TcaEstimate Create(
        SimplicitySnapshot snapshot,
        IEnumerable<FilterVerdict> filterVerdicts) =>
        Create(snapshot, filterVerdicts, TcaInputs.Defaults);

    /// <summary>
    /// Creates a TCA estimate with explicit calculation inputs.
    /// </summary>
    /// <param name="snapshot">The measured solution snapshot.</param>
    /// <param name="filterVerdicts">The Simplicity-First filter verdicts that feed the opportunity-cost model.</param>
    /// <param name="inputs">The business assumptions used by the annualized formulas.</param>
    /// <returns>The calculated annual estimate.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any required input is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when a required filter verdict is missing.</exception>
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

/// <summary>
/// Provides the configurable business inputs used to turn simplicity signals into annualized cost estimates.
/// </summary>
/// <param name="TeamSize">The number of engineers affected by the solution.</param>
/// <param name="AverageEngineerMonthlySalaryUsd">The average monthly engineer salary in USD used for annualized cost formulas.</param>
/// <param name="EstimatedMonthlyIncidentCount">The average incident count per month used for operational cost formulas.</param>
/// <param name="OnCallHourlyRateUsd">The hourly on-call rate in USD used for operational cost formulas.</param>
/// <param name="AttritionCoefficientPercent">The attrition pressure coefficient expressed as a percentage.</param>
public sealed record TcaInputs(
    int TeamSize,
    decimal AverageEngineerMonthlySalaryUsd,
    int EstimatedMonthlyIncidentCount,
    decimal OnCallHourlyRateUsd,
    decimal AttritionCoefficientPercent)
{
    /// <summary>
    /// Gets the built-in default assumptions used when callers do not provide explicit inputs.
    /// </summary>
    public static TcaInputs Defaults { get; } = new(
        TeamSize: 8,
        AverageEngineerMonthlySalaryUsd: 15000m,
        EstimatedMonthlyIncidentCount: 4,
        OnCallHourlyRateUsd: 150m,
        AttritionCoefficientPercent: 15m);
}

/// <summary>
/// Represents a low/high annualized money range in USD.
/// </summary>
/// <param name="Low">The low-end estimate.</param>
/// <param name="High">The high-end estimate.</param>
public readonly record struct MoneyRange(decimal Low, decimal High)
{
    /// <summary>
    /// Adds two money ranges component-wise.
    /// </summary>
    /// <param name="a">The first range.</param>
    /// <param name="b">The second range.</param>
    /// <returns>The summed range.</returns>
    public static MoneyRange operator +(MoneyRange a, MoneyRange b) =>
        new(a.Low + b.Low, a.High + b.High);

    /// <summary>
    /// Formats the range as invariant-culture USD text.
    /// </summary>
    /// <returns>The formatted money range.</returns>
    public override string ToString() =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"${Low:N0} - ${High:N0}");
}
