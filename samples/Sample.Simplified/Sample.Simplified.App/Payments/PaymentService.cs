namespace Sample.Simplified.App.Payments;

public sealed class PaymentService
{
    public PaymentAuthorization Authorize(PaymentMethod paymentMethod, decimal total)
    {
        if (total <= 0)
        {
            throw new InvalidOperationException("Order total must be positive.");
        }

        var prefix = paymentMethod == PaymentMethod.Card ? "CARD" : "INVOICE";
        var wholeDollars = decimal.ToInt32(decimal.Round(total, MidpointRounding.AwayFromZero));
        var captured = paymentMethod == PaymentMethod.Card;

        return new PaymentAuthorization($"{prefix}-{wholeDollars:0000}", captured);
    }
}
