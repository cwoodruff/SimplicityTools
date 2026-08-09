using Sample.ClaimsPortal.Api.Composition;
using Sample.ClaimsPortal.Claims;
using Sample.ClaimsPortal.Platform;
using Xunit;

namespace Sample.ClaimsPortal.Tests.EndToEnd;

/// <summary>
/// Exercises the full ten-frame intake chain end to end: endpoint -> intake service -> workflow
/// -> coordinator -> validator -> router -> gateway -> handler -> mapper -> store -> journal.
/// </summary>
public sealed class ClaimIntakeWorkflowTests
{
    private static ApiHost CreateHost() => new(FixedClock.On(2026, 6, 1));

    private static ClaimSubmission CleanAutoClaim() => new(
        "AUTO-1001",
        ClaimCategory.Auto,
        new Money(4_200m),
        new DateOnly(2026, 5, 20),
        "dana@example.test",
        "Rear-ended at a stop light.",
        SupportingDocumentCount: 3,
        ClaimsInLastNinetyDays: 0,
        AverageHistoricalClaim: new Money(3_000m));

    [Fact]
    public void Post_CleanAutoClaim_ReturnsOkAndPaysLedger()
    {
        var host = CreateHost();
        var context = host.NewRequestContext("test");

        var result = host.Claims.Post(CleanAutoClaim(), context);

        Assert.Equal(200, result.StatusCode);
        var claimNumber = result.Body.Split(' ')[0];
        Assert.Equal(new Money(3_700m), host.Ledger.TotalPostedFor(claimNumber));
    }

    [Fact]
    public void Post_CleanAutoClaim_WritesOneJournalEntry()
    {
        var host = CreateHost();

        host.Claims.Post(CleanAutoClaim(), host.NewRequestContext("test"));

        Assert.Single(host.JournalWriter.Lines);
        Assert.Contains("Approved", host.JournalWriter.Lines[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Post_MissingEmail_ReturnsBadRequestWithoutTouchingTheWorkflow()
    {
        var host = CreateHost();
        var submission = CleanAutoClaim() with { ClaimantEmail = "  " };

        var result = host.Claims.Post(submission, host.NewRequestContext("test"));

        Assert.Equal(400, result.StatusCode);
        Assert.Empty(host.JournalWriter.Lines);
    }

    [Fact]
    public void Get_AfterSubmission_FindsStoredDecision()
    {
        var host = CreateHost();
        var context = host.NewRequestContext("test");
        var posted = host.Claims.Post(CleanAutoClaim(), context);
        var claimNumber = posted.Body.Split(' ')[0];

        var fetched = host.Claims.Get(claimNumber, context);

        Assert.Equal(200, fetched.StatusCode);
        Assert.Contains(claimNumber, fetched.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void Get_UnknownClaim_ReturnsNotFound()
    {
        var host = CreateHost();

        var result = host.Claims.Get("AUT-999999", host.NewRequestContext("test"));

        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public void Post_ClaimantAlwaysReceivesExactlyOneNotification()
    {
        var host = CreateHost();

        host.Claims.Post(CleanAutoClaim(), host.NewRequestContext("test"));

        Assert.Single(host.Notifier.Sent);
    }
}
