using Sample.OverEngineered.Domain.Contracts;

namespace Sample.OverEngineered.Validation;

public sealed class OrderValidator : IOrderValidator
{
    private readonly IReadOnlyCollection<IValidationRule> _rules;

    public OrderValidator(IEnumerable<IValidationRule> rules)
    {
        _rules = rules.ToArray();
    }

    public void Validate(PlaceOrderInput input)
    {
        var failures = _rules
            .Select(rule => rule.Evaluate(input))
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Cast<string>()
            .ToArray();

        if (failures.Length > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
        }
    }
}
