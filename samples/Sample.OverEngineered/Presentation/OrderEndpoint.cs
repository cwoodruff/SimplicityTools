using Sample.OverEngineered.Application;
using Sample.OverEngineered.Domain.Contracts;

namespace Sample.OverEngineered.Presentation;

internal sealed class OrderEndpoint
{
    private readonly IPlaceOrderApplicationService _applicationService;

    public OrderEndpoint(IPlaceOrderApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    public string Post(PlaceOrderRequest request)
    {
        var input = new PlaceOrderInput(
            request.CustomerId,
            request.Lines.Select(line => new PlaceOrderLineInput(line.Sku, line.Quantity, line.UnitPrice)).ToArray(),
            request.Notes);

        var command = new PlaceOrderCommand(input, request.ActorId);
        var result = _applicationService.PlaceOrder(command);

        return $"{result.OrderNumber}: {result.Summary}";
    }
}
