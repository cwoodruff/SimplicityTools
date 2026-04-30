using PrimaryPathAnnotationFixture.App.Services;

namespace PrimaryPathAnnotationFixture.App.Workers;

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
