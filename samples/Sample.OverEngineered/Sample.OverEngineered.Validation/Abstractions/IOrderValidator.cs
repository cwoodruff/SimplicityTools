using Sample.OverEngineered.Domain.Contracts;

namespace Sample.OverEngineered.Validation;

public interface IOrderValidator
{
    void Validate(PlaceOrderInput input);
}
