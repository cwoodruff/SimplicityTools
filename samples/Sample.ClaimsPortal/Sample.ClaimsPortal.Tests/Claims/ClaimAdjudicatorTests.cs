using Sample.ClaimsPortal.Claims;
using Sample.ClaimsPortal.Claims.Adjudication;
using Sample.ClaimsPortal.Fraud;
using Sample.ClaimsPortal.Platform;
using Sample.ClaimsPortal.Policies;
using Xunit;

namespace Sample.ClaimsPortal.Tests.Claims;

public sealed class ClaimAdjudicatorTests
{
    private static readonly DateOnly Today = new(2026, 6, 1);

    private static Policy AutoPolicy() => new(
        "AUTO-1001",
        "Dana Reyes",
        new DateOnly(2026, 1, 1),
        new DateOnly(2026, 12, 31),
        [new Coverage(ClaimCategory.Auto, new Money(25_000m), new Money(500m))]);

    private static ClaimSubmission Submission(
        decimal amount = 5_000m,
        int documents = 3,
        int recentClaims = 0,
        int daysAgo = 10,
        ClaimCategory category = ClaimCategory.Auto) =>
        new(
            "AUTO-1001",
            category,
            new Money(amount),
            Today.AddDays(-daysAgo),
            "dana@example.test",
            "Test claim.",
            documents,
            recentClaims,
            new Money(4_000m));

    [Fact]
    public void Adjudicate_MissingPolicy_ReturnsDenied()
    {
        var decision = new ClaimAdjudicator().Adjudicate("AUT-000001", Submission(), policy: null, FraudScore.None, Today);

        Assert.Equal(ClaimStatus.Denied, decision.Status);
        Assert.Equal(Money.Zero, decision.ApprovedAmount);
    }

    [Fact]
    public void Adjudicate_CleanClaim_ApprovesAmountLessDeductible()
    {
        var decision = new ClaimAdjudicator().Adjudicate("AUT-000001", Submission(), AutoPolicy(), FraudScore.None, Today);

        Assert.Equal(ClaimStatus.Approved, decision.Status);
        Assert.Equal(new Money(4_500m), decision.ApprovedAmount);
    }

    [Fact]
    public void Adjudicate_AmountInsideDeductible_ReturnsDenied()
    {
        var decision = new ClaimAdjudicator().Adjudicate("AUT-000001", Submission(amount: 400m), AutoPolicy(), FraudScore.None, Today);

        Assert.Equal(ClaimStatus.Denied, decision.Status);
    }

    [Fact]
    public void Adjudicate_HighFraudScore_ReturnsManualReview()
    {
        var decision = new ClaimAdjudicator().Adjudicate("AUT-000001", Submission(), AutoPolicy(), new FraudScore(70), Today);

        Assert.Equal(ClaimStatus.ManualReview, decision.Status);
    }

    [Fact]
    public void Adjudicate_RepeatAutoClaims_ReducesPayoutToNinetyPercent()
    {
        var decision = new ClaimAdjudicator().Adjudicate("AUT-000001", Submission(recentClaims: 2), AutoPolicy(), FraudScore.None, Today);

        Assert.Equal(ClaimStatus.PartiallyApproved, decision.Status);
        Assert.Equal(new Money(4_050m), decision.ApprovedAmount);
    }

    [Fact]
    public void Adjudicate_AboveCoverageLimit_CapsAtLimit()
    {
        var decision = new ClaimAdjudicator().Adjudicate("AUT-000001", Submission(amount: 90_000m), AutoPolicy(), FraudScore.None, Today);

        Assert.Equal(ClaimStatus.PartiallyApproved, decision.Status);
        Assert.Equal(new Money(25_000m), decision.ApprovedAmount);
    }

    [Fact]
    public void Adjudicate_StaleIncident_ReturnsDenied()
    {
        var decision = new ClaimAdjudicator().Adjudicate("AUT-000001", Submission(daysAgo: 400), AutoPolicy(), FraudScore.None, Today);

        Assert.Equal(ClaimStatus.Denied, decision.Status);
    }

    [Fact]
    public void Adjudicate_UncoveredCategory_ReturnsDenied()
    {
        var decision = new ClaimAdjudicator().Adjudicate(
            "MED-000001",
            Submission(category: ClaimCategory.Medical),
            AutoPolicy(),
            FraudScore.None,
            Today);

        Assert.Equal(ClaimStatus.Denied, decision.Status);
    }
}
