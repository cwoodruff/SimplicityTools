using Sample.OverEngineered.Domain.Contracts;

namespace Sample.OverEngineered.Validation;

public interface IValidationRule
{
    string? Evaluate(PlaceOrderInput input);
}
