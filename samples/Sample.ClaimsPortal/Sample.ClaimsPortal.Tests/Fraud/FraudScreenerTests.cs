using Sample.ClaimsPortal.Fraud;
using Sample.ClaimsPortal.Platform;
using Xunit;

namespace Sample.ClaimsPortal.Tests.Fraud;

public sealed class FraudScreenerTests
{
    private static FraudContext Context(int recentClaims, decimal claimed, decimal average) =>
        new("AUTO-1001", ClaimCategory.Auto, new Money(claimed), recentClaims, new Money(average));

    [Fact]
    public void Screen_QuietHistory_ScoresZero()
    {
        var score = FraudScreener.CreateDefault().Screen(Context(recentClaims: 0, claimed: 3_000m, average: 3_000m));

        Assert.Equal(0, score.Value);
        Assert.False(score.RequiresManualReview);
    }

    [Fact]
    public void Screen_ManyRecentClaimsAndLargeAmount_RequiresManualReview()
    {
        var score = FraudScreener.CreateDefault().Screen(Context(recentClaims: 4, claimed: 60_000m, average: 2_500m));

        Assert.True(score.RequiresManualReview);
    }

    [Fact]
    public void Screen_NoHistoricalAverage_IgnoresAmountAnomaly()
    {
        var score = FraudScreener.CreateDefault().Screen(Context(recentClaims: 0, claimed: 90_000m, average: 0m));

        Assert.Equal(0, score.Value);
    }
}
