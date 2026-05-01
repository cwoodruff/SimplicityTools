using Sample.Simplified.App.Payments;
using Xunit;

namespace Sample.Simplified.Tests.Payments;

public sealed class PaymentServiceTests
{
    [Fact]
    public void Authorize_GeneratesPredictableApprovalCodes()
    {
        var service = new PaymentService();

        var authorization = service.Authorize(PaymentMethod.Card, 79.75m);

        Assert.Equal("CARD-0080", authorization.ApprovalCode);
        Assert.True(authorization.Captured);
    }
}
