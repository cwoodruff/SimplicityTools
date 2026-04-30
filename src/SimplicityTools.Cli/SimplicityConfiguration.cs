namespace SimplicityTools.Cli;

internal sealed record SimplicityConfiguration(
    TcaConfiguration Tca,
    FilterThresholdConfiguration Filters)
{
    public static SimplicityConfiguration Defaults { get; } = new(
        new TcaConfiguration(
            TeamSize: 8,
            AverageEngineerMonthlySalaryUsd: 15000m,
            EstimatedMonthlyIncidentCount: 4,
            OnCallHourlyRateUsd: 150m,
            AttritionCoefficientPercent: 15m),
        new FilterThresholdConfiguration(
            PrimaryPathRatioTarget: 0.60d,
            PrematureAbstractionRatioTarget: 0.25d,
            MaxMethodComplexity: 5d,
            MaxOnboardingHours: 40d,
            PassingScore: 0.70d));
}

internal sealed record TcaConfiguration(
    int TeamSize,
    decimal AverageEngineerMonthlySalaryUsd,
    int EstimatedMonthlyIncidentCount,
    decimal OnCallHourlyRateUsd,
    decimal AttritionCoefficientPercent);

internal sealed record FilterThresholdConfiguration(
    double PrimaryPathRatioTarget,
    double PrematureAbstractionRatioTarget,
    double MaxMethodComplexity,
    double MaxOnboardingHours,
    double PassingScore);
