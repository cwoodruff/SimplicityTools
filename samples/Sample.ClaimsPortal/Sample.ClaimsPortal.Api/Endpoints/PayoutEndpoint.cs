using Sample.ClaimsPortal.Api.Support;
using Sample.ClaimsPortal.Payments;

namespace Sample.ClaimsPortal.Api.Endpoints;

public sealed class PayoutEndpoint
{
    private readonly IPayoutLedger _ledger;
    private readonly EndpointLogger _logger;

    public PayoutEndpoint(IPayoutLedger ledger, EndpointLogger logger)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(logger);
        _ledger = ledger;
        _logger = logger;
    }

    public ApiResult Get(string claimNumber, RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.Begin(context, $"GET /payouts/{claimNumber}");

        var total = _ledger.TotalPostedFor(claimNumber);
        var result = ApiResult.Ok($"{claimNumber} paid {total}", context);

        _logger.End(context, result);
        return result;
    }
}
