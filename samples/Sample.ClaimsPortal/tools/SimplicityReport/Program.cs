// Library-level example of the three SimplicityTools NuGet packages:
//   SimplicityTools.Metrics  -> collect a snapshot
//   SimplicityTools.Filters  -> turn the snapshot into TwoAmTest / HalfRule / PrimaryPathFirst verdicts
//   SimplicityTools.Tca      -> price the excess complexity those verdicts describe
//
// Usage (from the repository root):
//   dotnet run --project samples/Sample.ClaimsPortal/tools/SimplicityReport
//   dotnet run --project samples/Sample.ClaimsPortal/tools/SimplicityReport -- path/to/Other.sln
//
// This project is deliberately NOT part of Sample.ClaimsPortal.sln: keeping it out means the
// numbers it prints describe the sample application, not the tooling that measures it.

using SimplicityTools.Filters;
using SimplicityTools.Metrics;
using SimplicityTools.Tca;

var solutionPath = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../..", "Sample.ClaimsPortal.sln"));

if (!File.Exists(solutionPath))
{
    Console.Error.WriteLine($"Solution not found: {solutionPath}");
    return 1;
}

Console.WriteLine($"Analyzing {solutionPath}");
Console.WriteLine();

// 1. Metrics — one call, one immutable snapshot.
var collector = new SimplicityCollector();
var snapshot = await collector.CollectAsync(solutionPath);

Console.WriteLine(snapshot.ToSummary());
Console.WriteLine();

// 2. Filters — thresholds mirror the filters section of simplicity.json.
var thresholds = new FilterThresholds(
    PrimaryPathRatioTarget: 0.60,
    PrematureAbstractionRatioTarget: 0.25,
    MaxMethodComplexity: 5.0,
    MaxOnboardingHours: 40.0,
    PassingScore: 0.70);

FilterVerdict[] verdicts =
[
    TwoAmTestEvaluator.Evaluate(snapshot, thresholds),
    HalfRuleEvaluator.Evaluate(snapshot, thresholds),
    PrimaryPathFirstEvaluator.Evaluate(snapshot, thresholds)
];

Console.WriteLine("Filter Verdicts");
Console.WriteLine("----------------------------------------");
foreach (var verdict in verdicts)
{
    Console.WriteLine($"{verdict.Filter,-17} {(verdict.Passes ? "PASS" : "FAIL")}  score {verdict.Score:F2}");
    Console.WriteLine($"  {verdict.Summary}");

    foreach (var violation in verdict.Violations)
    {
        Console.WriteLine($"  - {violation}");
    }

    foreach (var recommendation in verdict.Recommendations)
    {
        Console.WriteLine($"  -> {recommendation}");
    }
}

Console.WriteLine();
Console.WriteLine($"Simplicity score: {SimplicityScoring.CalculateScore(snapshot, thresholds):F0}/100");
Console.WriteLine();

// 3. TCA — the tca section of simplicity.json, expressed as code.
var inputs = new TcaInputs(
    TeamSize: 12,
    AverageEngineerMonthlySalaryUsd: 14_500m,
    EstimatedMonthlyIncidentCount: 6,
    OnCallHourlyRateUsd: 165m,
    AttritionCoefficientPercent: 18m);

var estimate = TcaEstimate.Create(snapshot, verdicts, inputs);
Console.WriteLine(estimate.ToExecutiveSummary());

return 0;
