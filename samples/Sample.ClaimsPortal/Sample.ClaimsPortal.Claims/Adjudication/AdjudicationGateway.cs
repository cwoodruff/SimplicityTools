using Sample.ClaimsPortal.Claims.Handlers;

namespace Sample.ClaimsPortal.Claims.Adjudication;

/// <summary>
/// A pass-through layer. It exists because "the handler should not be called directly", which is
/// exactly the kind of rule SF0004 is designed to make visible.
/// </summary>
public sealed class AdjudicationGateway
{
    private readonly AdjudicationHandler _handler;

    public AdjudicationGateway(AdjudicationHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handler = handler;
    }

    public ClaimDecision Dispatch(AdjudicationRequest request) => _handler.Handle(request);
}
