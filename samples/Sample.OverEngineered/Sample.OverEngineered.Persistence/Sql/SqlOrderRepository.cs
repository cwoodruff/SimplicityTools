using Sample.OverEngineered.Domain.Entities;
using Sample.OverEngineered.Infrastructure.Serialization;
using Sample.OverEngineered.Infrastructure.Time;

namespace Sample.OverEngineered.Persistence;

public sealed class SqlOrderRepository : IOrderRepository
{
    private readonly Dictionary<string, Order> _orders = new(StringComparer.OrdinalIgnoreCase);
    private readonly ISystemClock _systemClock;
    private readonly ITextSerializer _textSerializer;

    public SqlOrderRepository(ISystemClock systemClock, ITextSerializer textSerializer)
    {
        _systemClock = systemClock;
        _textSerializer = textSerializer;
    }

    public void Save(Order order)
    {
        _orders[order.OrderNumber] = order;
        _ = _systemClock.UtcNow;
        _ = _textSerializer.Serialize(order);
    }

    public Order? Find(string orderNumber)
    {
        _orders.TryGetValue(orderNumber, out var order);
        return order;
    }
}
