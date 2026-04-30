using Sample.Simplified.App.Composition;
using Sample.Simplified.App.Demo;

namespace Sample.Simplified.App;

internal static class Program
{
    private static void Main()
    {
        var host = AppHost.Create();
        var receipt = host.Orders.PlaceOrder(DemoScenario.CreateWeekendOrder());

        Console.WriteLine(receipt.ToSummary());
    }
}
