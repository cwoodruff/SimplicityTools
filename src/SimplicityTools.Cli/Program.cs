using SimplicityTools.Filters;
using SimplicityTools.Metrics;
using SimplicityTools.Tca;

var snapshot = SimplicitySnapshot.Empty("Scaffold");
var evaluation = new FilterEvaluation("Scaffold", Passed: true, "Toolkit scaffold is ready.", snapshot);
var estimate = new TcaEstimate(snapshot, [evaluation], EstimatedCost: 0m);

Console.WriteLine($"{estimate.Snapshot.SolutionName}: {estimate.FilterEvaluations[0].Summary}");
