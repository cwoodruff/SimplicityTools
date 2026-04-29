using Sample.OverEngineered.Domain.Contracts;

namespace Sample.OverEngineered.Validation;

public sealed class RequireCustomerIdRule : IValidationRule
{
    public string? Evaluate(PlaceOrderInput input)
    {
        return string.IsNullOrWhiteSpace(input.CustomerId)
            ? "Orders require a customer identifier before they enter the pipeline."
            : null;
    }
}
