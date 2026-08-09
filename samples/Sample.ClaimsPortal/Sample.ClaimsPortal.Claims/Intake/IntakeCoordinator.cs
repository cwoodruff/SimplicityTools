namespace Sample.ClaimsPortal.Claims.Intake;

public sealed class IntakeCoordinator
{
    private readonly SubmissionValidator _validator;

    public IntakeCoordinator(SubmissionValidator validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        _validator = validator;
    }

    public ClaimDecision Coordinate(string claimNumber, Envelope<ClaimSubmission> envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        return _validator.Validate(claimNumber, envelope.Payload);
    }
}
