using Sample.OverEngineered.Domain.Contracts;

namespace Sample.OverEngineered.WriteModel;

public interface IOrderDraftFactory
{
    OrderDraft Create(PlaceOrderInput input);
}
