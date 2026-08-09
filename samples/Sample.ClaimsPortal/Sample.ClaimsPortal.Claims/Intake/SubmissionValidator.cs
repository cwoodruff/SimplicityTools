using Sample.ClaimsPortal.Claims.Adjudication;
using Sample.ClaimsPortal.Platform;

namespace Sample.ClaimsPortal.Claims.Intake;

public sealed class SubmissionValidator
{
    private readonly TriageRouter _router;

    public SubmissionValidator(TriageRouter router)
    {
        ArgumentNullException.ThrowIfNull(router);
        _router = router;
    }

    public ClaimDecision Validate(string claimNumber, ClaimSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(submission);

        if (string.IsNullOrWhiteSpace(submission.PolicyNumber))
        {
            return ClaimDecision.Rejected(claimNumber, "Policy number is required.");
        }

        if (submission.ClaimedAmount.Amount <= 0m)
        {
            return ClaimDecision.Rejected(claimNumber, "Claimed amount must be positive.");
        }

        return _router.Route(new AdjudicationRequest(claimNumber, submission));
    }
}
