using System.Collections.Concurrent;
using Sample.OverEngineered.Cache;
using Sample.OverEngineered.Domain.Entities;

namespace Sample.OverEngineered.ReadModel;

public sealed class OrderReadModelStore : IOrderReadModelStore
{
    private readonly ConcurrentDictionary<string, OrderReadModel> _models = new(StringComparer.OrdinalIgnoreCase);
    private readonly ICacheStore _cacheStore;
    private readonly ICacheKeyBuilder _cacheKeyBuilder;

    public OrderReadModelStore(ICacheStore cacheStore, ICacheKeyBuilder cacheKeyBuilder)
    {
        _cacheStore = cacheStore;
        _cacheKeyBuilder = cacheKeyBuilder;
    }

    public OrderReadModel Upsert(Order order)
    {
        var model = new OrderReadModel(order.OrderNumber, order.CustomerId, order.Total.Amount, "Projected");
        _models[order.OrderNumber] = model;
        _cacheStore.Set(_cacheKeyBuilder.Build(order.OrderNumber), model.Status);
        return model;
    }

    public OrderReadModel? Get(string orderNumber)
    {
        _models.TryGetValue(orderNumber, out var model);
        return model;
    }
}
