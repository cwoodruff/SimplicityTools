using Sample.ClaimsPortal.Api.Support;
using Sample.ClaimsPortal.Claims;
using Sample.ClaimsPortal.Claims.Intake;

namespace Sample.ClaimsPortal.Api.Endpoints;

/// <summary>
/// Primary-path code: the Endpoints/ folder is one of the conventional primary-path segments the
/// metrics collector and SF0007 recognise.
/// </summary>
public sealed class ClaimsEndpoint
{
    private readonly ClaimIntakeService _intake;
    private readonly EndpointLogger _logger;

    public ClaimsEndpoint(ClaimIntakeService intake, EndpointLogger logger)
    {
        ArgumentNullException.ThrowIfNull(intake);
        ArgumentNullException.ThrowIfNull(logger);
        _intake = intake;
        _logger = logger;
    }

    public ApiResult Post(ClaimSubmission submission, RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(submission);
        ArgumentNullException.ThrowIfNull(context);

        _logger.Begin(context, "POST /claims");

        if (string.IsNullOrWhiteSpace(submission.ClaimantEmail))
        {
            var invalid = ApiResult.BadRequest("Claimant email is required.", context);
            _logger.End(context, invalid);
            return invalid;
        }

        var decision = _intake.Intake(submission, context.CorrelationId);
        var result = decision.IsPayable
            ? ApiResult.Ok($"{decision.ClaimNumber} {decision.Status} {decision.ApprovedAmount}", context)
            : ApiResult.Accepted($"{decision.ClaimNumber} {decision.Status}", context);

        _logger.End(context, result);
        return result;
    }

    public ApiResult Get(string claimNumber, RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.Begin(context, $"GET /claims/{claimNumber}");

        var decision = _intake.Lookup(claimNumber);
        var result = decision is null
            ? ApiResult.NotFound(context)
            : ApiResult.Ok($"{decision.ClaimNumber} {decision.Status} {decision.Reason}", context);

        _logger.End(context, result);
        return result;
    }
}
