using Sample.ClaimsPortal.Api.Composition;
using Xunit;

namespace Sample.ClaimsPortal.Tests.EndToEnd;

public sealed class StartupSmokeTests
{
    [Fact]
    public void ApiHost_Constructs_WithoutThrowing()
    {
        var host = new ApiHost(FixedClock.On(2026, 6, 1));

        Assert.NotNull(host.Claims);
        Assert.NotNull(host.Payouts);
        Assert.Empty(host.Logger.Entries);
    }
}
