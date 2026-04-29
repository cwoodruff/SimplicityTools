using SimplicityTools.Filters;
using SimplicityTools.Metrics;
using SimplicityTools.Tca;
using Xunit;

namespace SimplicityTools.Tca.Tests;

public sealed class TcaEstimateTests
{
    [Fact]
    public void Estimate_PreservesComputedCost()
    {
        var snapshot = SimplicitySnapshot.Empty("Sample");
        var evaluation = new FilterEvaluation("Half-Rule", Passed: true, "Placeholder", snapshot);
        var estimate = new TcaEstimate(snapshot, [evaluation], EstimatedCost: 42m);

        Assert.Equal(42m, estimate.EstimatedCost);
    }
}
