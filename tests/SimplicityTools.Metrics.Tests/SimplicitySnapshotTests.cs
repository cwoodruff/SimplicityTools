using SimplicityTools.Metrics;
using Xunit;

namespace SimplicityTools.Metrics.Tests;

public sealed class SimplicitySnapshotTests
{
    [Fact]
    public void Empty_SeedsSnapshotWithName()
    {
        var snapshot = SimplicitySnapshot.Empty("Sample");

        Assert.Equal("Sample", snapshot.SolutionName);
        Assert.Empty(snapshot.Metrics);
    }
}
