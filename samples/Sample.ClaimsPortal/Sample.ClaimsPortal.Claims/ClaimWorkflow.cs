using Sample.ClaimsPortal.Claims.Intake;

namespace Sample.ClaimsPortal.Claims;

/// <summary>
/// Top of the call chain SF0004 measures:
/// Submit -> Coordinate -> Validate -> Route -> Dispatch -> Handle -> Map -> Save -> Append -> Write.
/// Ten frames to answer one question: is this claim payable?
/// </summary>
public sealed class ClaimWorkflow
{
    private readonly IntakeCoordinator _coordinator;

    public ClaimWorkflow(IntakeCoordinator coordinator)
    {
        ArgumentNullException.ThrowIfNull(coordinator);
        _coordinator = coordinator;
    }

    public ClaimDecision Submit(string claimNumber, Envelope<ClaimSubmission> envelope) =>
        _coordinator.Coordinate(claimNumber, envelope);
}
