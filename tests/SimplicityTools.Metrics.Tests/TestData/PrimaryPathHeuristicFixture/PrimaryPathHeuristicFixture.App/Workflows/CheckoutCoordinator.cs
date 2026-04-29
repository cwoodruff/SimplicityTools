using PrimaryPathHeuristicFixture.App.Services;

namespace PrimaryPathHeuristicFixture.App.Workflows;

public sealed class CheckoutCoordinator
{
    private readonly CheckoutService checkoutService;

    public CheckoutCoordinator(CheckoutService checkoutService)
    {
        this.checkoutService = checkoutService;
    }

    public void Coordinate()
    {
        _ = checkoutService;
    }
}
