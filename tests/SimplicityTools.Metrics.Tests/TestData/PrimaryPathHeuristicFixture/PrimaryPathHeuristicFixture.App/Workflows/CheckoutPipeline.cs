using PrimaryPathHeuristicFixture.App.Services;

namespace PrimaryPathHeuristicFixture.App.Workflows;

public sealed class CheckoutPipeline
{
    private readonly CheckoutService checkoutService;

    public CheckoutPipeline(CheckoutService checkoutService)
    {
        this.checkoutService = checkoutService;
    }

    public void Advance()
    {
        _ = checkoutService;
    }
}
