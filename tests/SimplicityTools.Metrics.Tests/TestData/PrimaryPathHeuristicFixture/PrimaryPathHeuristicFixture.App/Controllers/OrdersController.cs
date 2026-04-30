using PrimaryPathHeuristicFixture.App.Services;

namespace PrimaryPathHeuristicFixture.App.Controllers;

public sealed class OrdersController(CheckoutService checkoutService)
{
    public void Post()
    {
        checkoutService.Run();
    }
}
