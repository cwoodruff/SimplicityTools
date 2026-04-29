using SimplicityTools.Analyzers;
using Xunit;

namespace SimplicityTools.Analyzers.Tests;

public sealed class PlaceholderAnalyzerTests
{
    [Fact]
    public void DiagnosticId_RemainsStable()
    {
        Assert.Equal("ST0001", PlaceholderAnalyzer.DiagnosticId);
    }
}
