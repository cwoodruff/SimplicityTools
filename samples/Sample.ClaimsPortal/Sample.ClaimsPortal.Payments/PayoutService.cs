using Sample.ClaimsPortal.Platform;
using Sample.ClaimsPortal.Platform.Telemetry;

namespace Sample.ClaimsPortal.Payments;

public sealed class PayoutService
{
    private readonly IPayoutLedger _ledger;
    private readonly ITelemetrySink _telemetry;

    public PayoutService(IPayoutLedger ledger, ITelemetrySink telemetry)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(telemetry);
        _ledger = ledger;
        _telemetry = telemetry;
    }

    public PayoutResult Settle(PayoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Amount.Amount <= 0m)
        {
            return new PayoutResult(request.ClaimNumber, Money.Zero, Settled: false, "Nothing to pay.");
        }

        _ledger.Post(request.ClaimNumber, request.Amount);
        _telemetry.Record("payout.settled", new Dictionary<string, string>
        {
            ["claim"] = request.ClaimNumber,
            ["amount"] = request.Amount.ToString()
        });

        return new PayoutResult(
            request.ClaimNumber,
            request.Amount,
            Settled: true,
            $"Paid {request.Amount} to {request.PayeeName}.");
    }
}
