using Sample.OverEngineered.Domain.Contracts;
using Sample.OverEngineered.Domain.Entities;
using Sample.OverEngineered.Infrastructure.Correlation;
using Sample.OverEngineered.Infrastructure.Time;

namespace Sample.OverEngineered.WriteModel;

public sealed class OrderDraftFactory : IOrderDraftFactory
{
    private readonly ISystemClock _systemClock;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;

    public OrderDraftFactory(ISystemClock systemClock, ICorrelationIdAccessor correlationIdAccessor)
    {
        _systemClock = systemClock;
        _correlationIdAccessor = correlationIdAccessor;
    }

    public OrderDraft Create(PlaceOrderInput input)
    {
        var lines = input.Lines.Select(line => new OrderLine(line.Sku, line.Quantity, line.UnitPrice)).ToArray();
        return new OrderDraft(input.CustomerId, lines, input.Notes, _systemClock.UtcNow, _correlationIdAccessor.GetCurrentCorrelationId());
    }
}
