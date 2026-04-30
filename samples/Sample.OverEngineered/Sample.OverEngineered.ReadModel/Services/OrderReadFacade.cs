namespace Sample.OverEngineered.ReadModel;

public sealed class OrderReadFacade : IOrderReadFacade
{
    private readonly IOrderReadModelStore _orderReadModelStore;

    public OrderReadFacade(IOrderReadModelStore orderReadModelStore)
    {
        _orderReadModelStore = orderReadModelStore;
    }

    public OrderReadModel? Get(string orderNumber) => _orderReadModelStore.Get(orderNumber);
}
