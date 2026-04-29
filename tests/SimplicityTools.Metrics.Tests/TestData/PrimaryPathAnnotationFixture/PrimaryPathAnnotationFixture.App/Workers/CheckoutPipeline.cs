using PrimaryPathAnnotationFixture.App.Services;

namespace PrimaryPathAnnotationFixture.App.Workers;

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
