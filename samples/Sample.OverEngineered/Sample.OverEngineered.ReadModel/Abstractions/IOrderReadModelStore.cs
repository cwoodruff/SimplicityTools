using Sample.OverEngineered.Domain.Entities;

namespace Sample.OverEngineered.ReadModel;

public interface IOrderReadModelStore
{
    OrderReadModel Upsert(Order order);

    OrderReadModel? Get(string orderNumber);
}
