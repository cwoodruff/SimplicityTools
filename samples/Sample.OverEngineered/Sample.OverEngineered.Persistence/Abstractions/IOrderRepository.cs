using Sample.OverEngineered.Domain.Entities;

namespace Sample.OverEngineered.Persistence;

public interface IOrderRepository
{
    void Save(Order order);

    Order? Find(string orderNumber);
}
