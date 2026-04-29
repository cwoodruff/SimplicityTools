using Sample.OverEngineered.Domain.Contracts;
using Sample.OverEngineered.Domain.Entities;
using Sample.OverEngineered.Domain.Services;

namespace Sample.OverEngineered.WriteModel;

public sealed class OrderWriteFacade : IOrderWriteFacade
{
    private readonly IOrderDraftFactory _orderDraftFactory;
    private readonly IOrderNumberGenerator _orderNumberGenerator;

    public OrderWriteFacade(IOrderDraftFactory orderDraftFactory, IOrderNumberGenerator orderNumberGenerator)
    {
        _orderDraftFactory = orderDraftFactory;
        _orderNumberGenerator = orderNumberGenerator;
    }

    public Order CreateOrder(PlaceOrderInput input)
    {
        var draft = _orderDraftFactory.Create(input);
        return new Order(_orderNumberGenerator.Generate(), draft.CustomerId, draft.Lines, draft.Notes, draft.CapturedAt);
    }
}
