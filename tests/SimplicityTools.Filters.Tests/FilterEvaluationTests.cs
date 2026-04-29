using SimplicityTools.Filters;
using SimplicityTools.Metrics;
using Xunit;

namespace SimplicityTools.Filters.Tests;

public sealed class FilterEvaluationTests
{
    [Fact]
    public void Evaluation_HoldsSnapshotReference()
    {
        var snapshot = SimplicitySnapshot.Empty("Sample");
        var evaluation = new FilterEvaluation("2AM", Passed: true, "Placeholder", snapshot);

        Assert.Same(snapshot, evaluation.Snapshot);
    }
}
