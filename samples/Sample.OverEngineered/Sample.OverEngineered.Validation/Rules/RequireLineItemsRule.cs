using Sample.OverEngineered.Domain.Contracts;

namespace Sample.OverEngineered.Validation;

public sealed class RequireLineItemsRule : IValidationRule
{
    public string? Evaluate(PlaceOrderInput input)
    {
        return input.Lines.Count == 0
            ? "Orders require at least one line item before they enter the pipeline."
            : null;
    }
}
