using System.Globalization;
using SimplicityTools.Filters;
using SimplicityTools.Metrics;

namespace SimplicityTools.Tca;

/// <summary>
/// Represents the annual Total Cost of Architecture estimate broken down by major cost category.
/// Every category charges only the excess over the Simplicity-First target for its driving metric,
/// so a snapshot that meets every target reports $0 architecture-attributed cost. The unavoidable
/// at-target spend is reported separately as <see cref="BaselineOperatingCostPerYear" />.
/// </summary>
/// <param name="InfrastructureCostPerYear">The estimated annual infrastructure cost range attributed to excess complexity.</param>
/// <param name="OperationalCostPerYear">The estimated annual operational cost range attributed to excess complexity.</param>
/// <param name="CoordinationCostPerYear">The estimated annual coordination cost range attributed to excess complexity.</param>
/// <param name="CognitiveCostPerYear">The estimated annual cognitive load cost range attributed to excess complexity.</param>
/// <param name="OpportunityCostPerYear">The estimated annual opportunity cost range attributed to excess complexity.</param>
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
    /// Gets the estimated annual operating cost a solution would still incur if it sat exactly at
    /// every simplicity target (infrastructure spend for its projects plus coordination for up to
    /// the baseline project count). This spend is normal cost of doing business and is never
    /// attributed to architecture excess.
    /// </summary>
    public decimal BaselineOperatingCostPerYear { get; init; }

    /// <summary>
    /// Gets explanatory notes produced during calculation, such as metrics that were not measured
    /// or were not finite numbers and therefore contributed zero excess.
    /// </summary>
    public IReadOnlyList<string> Notes { get; init; } = [];

    /// <summary>
    /// Formats the estimate as the multi-line executive summary used by the CLI and documentation samples.
    /// </summary>
    /// <returns>A multi-line summary of the annual estimate.</returns>
    public string ToExecutiveSummary()
    {
        var baseline = BaselineOperatingCostPerYear.ToString("N0", CultureInfo.InvariantCulture);
        var lines = new List<string>
        {
            "Total Cost of Architecture (Annual Estimate)",
            "============================================",
            "Architecture excess over simplicity targets:",
            $"Infrastructure:   {InfrastructureCostPerYear}",
            $"Operational:      {OperationalCostPerYear}",
            $"Coordination:     {CoordinationCostPerYear}",
            $"Cognitive:        {CognitiveCostPerYear}",
            $"Opportunity:      {OpportunityCostPerYear}",
            "--------------------------------------------",
            $"TOTAL EXCESS:     {TotalPerYear} per year",
            $"Baseline operating cost at target: ${baseline} per year (not attributed to architecture)"
        };

        lines.AddRange(Notes.Select(static note => $"Note: {note}"));

        return string.Join(Environment.NewLine, lines);
    }

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
    /// <exception cref="ArgumentException">Thrown when a required filter verdict is missing or two verdicts for the same filter conflict.</exception>
    public static TcaEstimate Create(
        SimplicitySnapshot snapshot,
        IEnumerable<FilterVerdict> filterVerdicts,
        TcaInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(filterVerdicts);
        ArgumentNullException.ThrowIfNull(inputs);

        var verdictsByFilter = IndexVerdicts(filterVerdicts);
        var notes = new List<string>();

        var infrastructureAnnual = CalculateInfrastructureExcess(snapshot, inputs);
        var operationalAnnual = CalculateOperationalExcess(snapshot, inputs, notes);
        var coordinationAnnual = CalculateCoordinationExcess(snapshot, inputs);
        var cognitiveAnnual = CalculateCognitiveExcess(snapshot, inputs, notes);
        var opportunityAnnual = CalculateOpportunityExcess(verdictsByFilter, inputs, notes);

        return new TcaEstimate(
            CreateRange(infrastructureAnnual, inputs),
            CreateRange(operationalAnnual, inputs),
            CreateRange(coordinationAnnual, inputs),
            CreateRange(cognitiveAnnual, inputs),
            CreateRange(opportunityAnnual, inputs))
        {
            BaselineOperatingCostPerYear = CalculateBaselineOperatingCost(snapshot, inputs),
            Notes = notes
        };
    }

    private static decimal CalculateInfrastructureExcess(SimplicitySnapshot snapshot, TcaInputs inputs)
    {
        var baseline = InfrastructureBaseline(snapshot, inputs);
        var excessFactor = Math.Min(
            NonNegative(inputs.MaxExcessFactor),
            Math.Max(0, snapshot.UnusedDependencyCount) * NonNegative(inputs.InfrastructureExcessPerUnusedDependency));

        return baseline * excessFactor;
    }

    private static decimal CalculateOperationalExcess(SimplicitySnapshot snapshot, TcaInputs inputs, List<string> notes)
    {
        var complexity = SafeMetric(snapshot.AverageMethodComplexity, "Average method complexity", notes);

        return ExcessFactor(complexity, inputs.TargetAverageMethodComplexity, inputs)
            * NonNegative(inputs.OnCallHourlyRateUsd)
            * Math.Max(0, inputs.EstimatedMonthlyIncidentCount)
            * 12m
            * NonNegative(inputs.IncidentCostMultiplier);
    }

    private static decimal CalculateCoordinationExcess(SimplicitySnapshot snapshot, TcaInputs inputs)
        => ExcessFactor(Math.Max(0, snapshot.TotalProjects), inputs.BaselineProjectCount, inputs)
            * Math.Max(0, inputs.BaselineProjectCount)
            * NonNegative(inputs.MonthlyCoordinationCostPerProjectUsd)
            * 12m;

    private static decimal CalculateCognitiveExcess(SimplicitySnapshot snapshot, TcaInputs inputs, List<string> notes)
    {
        decimal excessFactor;
        if (snapshot.EstimatedOnboardingTime is { } measured)
        {
            var onboardingHours = SafeMetric(measured.TotalHours, "Estimated onboarding time", notes);
            excessFactor = ExcessFactor(onboardingHours, inputs.TargetOnboardingHours, inputs);
        }
        else
        {
            excessFactor = 0m;
            notes.Add("Onboarding time was not measured; the cognitive category reports $0 excess.");
        }

        var uplift = 1m + (SafeMetric(snapshot.PrematureAbstractionRatio, "Premature abstraction ratio", notes)
            * NonNegative(inputs.PrematureAbstractionUpliftFactor));

        return excessFactor
            * NonNegative(inputs.AverageEngineerMonthlySalaryUsd)
            * 12m
            * (NonNegative(inputs.AttritionCoefficientPercent) / 100m)
            * Math.Max(0, inputs.TeamSize)
            * uplift;
    }

    private static decimal CalculateOpportunityExcess(
        IReadOnlyDictionary<FilterName, FilterVerdict> verdictsByFilter,
        TcaInputs inputs,
        List<string> notes)
    {
        var compositeScore = (
            SafeScore(GetRequiredVerdict(verdictsByFilter, FilterName.TwoAmTest), notes) +
            SafeScore(GetRequiredVerdict(verdictsByFilter, FilterName.HalfRule), notes) +
            SafeScore(GetRequiredVerdict(verdictsByFilter, FilterName.PrimaryPathFirst), notes)) / 3m;

        return Math.Max(0, inputs.TeamSize)
            * NonNegative(inputs.AverageEngineerMonthlySalaryUsd)
            * 12m
            * (1.0m - compositeScore)
            * NonNegative(inputs.PayrollOpportunityFactor);
    }

    private static decimal CalculateBaselineOperatingCost(SimplicitySnapshot snapshot, TcaInputs inputs)
    {
        var coordinatedProjects = Math.Min(Math.Max(0, snapshot.TotalProjects), Math.Max(0, inputs.BaselineProjectCount));

        return InfrastructureBaseline(snapshot, inputs)
            + (coordinatedProjects * NonNegative(inputs.MonthlyCoordinationCostPerProjectUsd) * 12m);
    }

    private static decimal InfrastructureBaseline(SimplicitySnapshot snapshot, TcaInputs inputs)
        => Math.Max(0, snapshot.TotalProjects) * NonNegative(inputs.MonthlyInfrastructureCostPerProjectUsd) * 12m;

    private static Dictionary<FilterName, FilterVerdict> IndexVerdicts(IEnumerable<FilterVerdict> filterVerdicts)
    {
        var verdictsByFilter = new Dictionary<FilterName, FilterVerdict>();

        foreach (var verdict in filterVerdicts)
        {
            if (verdictsByFilter.TryAdd(verdict.Filter, verdict))
            {
                continue;
            }

            var existing = verdictsByFilter[verdict.Filter];
            if (existing.Passes != verdict.Passes || !existing.Score.Equals(verdict.Score))
            {
                throw new ArgumentException(
                    $"Conflicting verdicts were provided for {verdict.Filter}: scores {existing.Score} and {verdict.Score}.",
                    nameof(filterVerdicts));
            }
        }

        return verdictsByFilter;
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

    private static decimal ExcessFactor(decimal metric, decimal target, TcaInputs inputs)
        => target <= 0m
            ? 0m
            : Math.Min(NonNegative(inputs.MaxExcessFactor), Math.Max(0m, (metric - target) / target));

    private static decimal SafeMetric(double value, string metricName, List<string> notes)
    {
        if (!double.IsFinite(value))
        {
            notes.Add($"{metricName} was not a finite number; it contributed zero excess.");
            return 0m;
        }

        return value < 0d ? 0m : (decimal)value;
    }

    private static decimal SafeScore(FilterVerdict verdict, List<string> notes)
    {
        if (!double.IsFinite(verdict.Score))
        {
            notes.Add($"The {verdict.Filter} score was not a finite number; it contributed zero excess.");
            return 1m;
        }

        return Math.Clamp((decimal)verdict.Score, 0m, 1m);
    }

    private static decimal NonNegative(decimal value)
        => Math.Max(0m, value);

    private static MoneyRange CreateRange(decimal annualCost, TcaInputs inputs)
        => new(
            annualCost * NonNegative(inputs.RangeLowMultiplier),
            annualCost * NonNegative(inputs.RangeHighMultiplier));
}

/// <summary>
/// Provides the configurable business inputs used to turn simplicity signals into annualized cost
/// estimates. The positional parameters describe the team; the init-only properties are the model
/// constants (with documented rationale) that previously lived inline in the formulas.
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
    /// The assumed monthly infrastructure spend per project in USD (build agents, hosting,
    /// pipelines, tooling seats). This is a rough industry placeholder, not a benchmark; replace it
    /// with your actual per-project platform spend for a defensible estimate. Default: $200.
    /// </summary>
    public decimal MonthlyInfrastructureCostPerProjectUsd { get; init; } = 200m;

    /// <summary>
    /// The fraction of the infrastructure baseline charged as architecture excess per unused
    /// package dependency. Each dead dependency still costs restore time, scanning, and upgrade
    /// churn; 0.05 assumes each one wastes about 5% of a project's platform budget. The combined
    /// factor is capped by <see cref="MaxExcessFactor" />. Default: 0.05.
    /// </summary>
    public decimal InfrastructureExcessPerUnusedDependency { get; init; } = 0.05m;

    /// <summary>
    /// The multiplier applied to the on-call bill per incident to account for costs beyond the
    /// responder's hours: interrupted teammates, context-switch recovery, and follow-up work. The
    /// default of 4 assumes roughly three additional engineer-hours are consumed around every
    /// on-call hour. Default: 4.
    /// </summary>
    public decimal IncidentCostMultiplier { get; init; } = 4m;

    /// <summary>
    /// The assumed monthly cross-team coordination cost per project in USD (meetings, release
    /// synchronization, cross-repo changes). The default approximates a few engineer-days of
    /// coordination overhead per project per month at typical loaded rates. Default: $4,000.
    /// </summary>
    public decimal MonthlyCoordinationCostPerProjectUsd { get; init; } = 4000m;

    /// <summary>
    /// The number of projects a solution can have before coordination overhead is attributed to
    /// architecture. Up to this count, coordination is treated as baseline operating cost.
    /// Default: 3.
    /// </summary>
    public int BaselineProjectCount { get; init; } = 3;

    /// <summary>
    /// The uplift applied to the cognitive excess per unit of premature-abstraction ratio.
    /// Single-implementation interfaces force newcomers to chase indirection while learning the
    /// code; 0.5 means a fully premature abstraction layer inflates onboarding cost by 50%.
    /// Default: 0.5.
    /// </summary>
    public decimal PrematureAbstractionUpliftFactor { get; init; } = 0.5m;

    /// <summary>
    /// The fraction of annual payroll treated as redirectable engineering capacity when filter
    /// scores fall below a perfect 1.0. The default assumes at most 40% of engineering time is
    /// discretionary feature work that complexity can crowd out; the remainder is meetings,
    /// support, and unavoidable overhead. Default: 0.4.
    /// </summary>
    public decimal PayrollOpportunityFactor { get; init; } = 0.4m;

    /// <summary>
    /// The multiplier producing the low end of every reported range, acknowledging that these are
    /// order-of-magnitude estimates rather than measurements. Default: 0.7 (30% below the point
    /// estimate).
    /// </summary>
    public decimal RangeLowMultiplier { get; init; } = 0.7m;

    /// <summary>
    /// The multiplier producing the high end of every reported range. Default: 1.3 (30% above the
    /// point estimate).
    /// </summary>
    public decimal RangeHighMultiplier { get; init; } = 1.3m;

    /// <summary>
    /// The cap applied to every excess-over-target factor so no single dimension scales without
    /// bound. A metric more than (1 + cap) times its target is charged as if it were exactly at
    /// the cap; beyond that point the model's linear assumptions stop being credible. Default: 3.0
    /// (a dimension is charged at most three times its at-target reference cost).
    /// </summary>
    public decimal MaxExcessFactor { get; init; } = 3.0m;

    /// <summary>
    /// The average method complexity treated as healthy. Matches the Simplicity-First
    /// diagnosability target used by the TwoAmTest filter; only complexity above this value is
    /// charged. Default: 5.
    /// </summary>
    public decimal TargetAverageMethodComplexity { get; init; } = 5m;

    /// <summary>
    /// The onboarding-hours budget treated as healthy (one working week). Matches the
    /// Simplicity-First cognitive-load target used by the TwoAmTest filter; only hours above this
    /// value are charged. Default: 40.
    /// </summary>
    public decimal TargetOnboardingHours { get; init; } = 40m;

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
