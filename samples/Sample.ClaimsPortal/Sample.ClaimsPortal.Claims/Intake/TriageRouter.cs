using Sample.ClaimsPortal.Claims.Adjudication;

namespace Sample.ClaimsPortal.Claims.Intake;

public sealed class TriageRouter
{
    private readonly AdjudicationGateway _gateway;

    public TriageRouter(AdjudicationGateway gateway)
    {
        ArgumentNullException.ThrowIfNull(gateway);
        _gateway = gateway;
    }

    public ClaimDecision Route(AdjudicationRequest request) => _gateway.Dispatch(request);
}
