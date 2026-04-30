using PrimaryPathAnnotationFixture.App.Services;

namespace PrimaryPathAnnotationFixture.App.Controllers;

public sealed class OrdersController(CheckoutService checkoutService)
{
    public void Post()
    {
        checkoutService.Run();
    }
}
