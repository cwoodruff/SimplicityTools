using Sample.ClaimsPortal.Payments;
using Sample.ClaimsPortal.Platform;
using Sample.ClaimsPortal.Platform.Telemetry;
using Xunit;

namespace Sample.ClaimsPortal.Tests.Payments;

public sealed class PayoutServiceTests
{
    [Fact]
    public void Settle_PositiveAmount_PostsToLedger()
    {
        var ledger = new InMemoryPayoutLedger();
        var service = new PayoutService(ledger, new ConsoleTelemetrySink());

        var result = service.Settle(new PayoutRequest("AUT-000001", "Dana Reyes", new Money(4_500m)));

        Assert.True(result.Settled);
        Assert.Equal(new Money(4_500m), ledger.TotalPostedFor("AUT-000001"));
    }

    [Fact]
    public void Settle_ZeroAmount_DoesNotPost()
    {
        var ledger = new InMemoryPayoutLedger();
        var service = new PayoutService(ledger, new ConsoleTelemetrySink());

        var result = service.Settle(new PayoutRequest("AUT-000002", "Dana Reyes", Money.Zero));

        Assert.False(result.Settled);
        Assert.Equal(Money.Zero, ledger.TotalPostedFor("AUT-000002"));
    }
}
