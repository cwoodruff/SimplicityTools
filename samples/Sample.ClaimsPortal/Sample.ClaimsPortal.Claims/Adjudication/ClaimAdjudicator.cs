using Sample.ClaimsPortal.Fraud;
using Sample.ClaimsPortal.Platform;
using Sample.ClaimsPortal.Policies;

namespace Sample.ClaimsPortal.Claims.Adjudication;

/// <summary>
/// SF0003 hit. <see cref="Adjudicate" /> has a cyclomatic complexity well above the default
/// threshold of 10 — this is the "can you understand it at 2am?" method. Every rule the business
/// added over three years landed in the same block.
/// </summary>
public sealed class ClaimAdjudicator
{
    public ClaimDecision Adjudicate(
        string claimNumber,
        ClaimSubmission submission,
        Policy? policy,
        FraudScore fraudScore,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(submission);

        if (policy is null)
        {
            return ClaimDecision.Rejected(claimNumber, "No policy on file.");
        }

        if (!policy.IsActiveOn(submission.IncidentDate))
        {
            return ClaimDecision.Rejected(claimNumber, "Policy was not active on the incident date.");
        }

        var coverage = policy.CoverageFor(submission.Category);
        if (coverage is null)
        {
            return ClaimDecision.Rejected(claimNumber, $"Policy does not cover {submission.Category}.");
        }

        if (submission.IncidentDate > today)
        {
            return ClaimDecision.Rejected(claimNumber, "Incident date is in the future.");
        }

        var ageInDays = today.DayNumber - submission.IncidentDate.DayNumber;
        if (ageInDays > 365)
        {
            return ClaimDecision.Rejected(claimNumber, "Reported more than a year after the incident.");
        }

        if (fraudScore.RequiresManualReview)
        {
            return new ClaimDecision(claimNumber, ClaimStatus.ManualReview, Money.Zero, $"Fraud score {fraudScore.Value}.");
        }

        if (submission.SupportingDocumentCount == 0 && submission.ClaimedAmount > new Money(1_000m))
        {
            return new ClaimDecision(claimNumber, ClaimStatus.ManualReview, Money.Zero, "Documentation required above $1,000.");
        }

        var payable = submission.ClaimedAmount - coverage.Deductible;
        if (payable < Money.Zero)
        {
            payable = Money.Zero;
        }

        if (payable == Money.Zero)
        {
            return ClaimDecision.Rejected(claimNumber, "Claim falls entirely inside the deductible.");
        }

        var capped = false;
        if (payable > coverage.PerClaimLimit)
        {
            payable = coverage.PerClaimLimit;
            capped = true;
        }

        if (submission.Category == ClaimCategory.Medical && ageInDays > 180)
        {
            payable *= 0.8m;
            capped = true;
        }
        else if (submission.Category == ClaimCategory.Auto && submission.ClaimsInLastNinetyDays >= 2)
        {
            payable *= 0.9m;
            capped = true;
        }
        else if (submission.Category == ClaimCategory.Property && submission.SupportingDocumentCount < 2)
        {
            payable *= 0.75m;
            capped = true;
        }

        if (fraudScore.Value >= 30 && payable > new Money(10_000m))
        {
            return new ClaimDecision(claimNumber, ClaimStatus.ManualReview, Money.Zero, "Large payout with elevated fraud score.");
        }

        return capped
            ? new ClaimDecision(claimNumber, ClaimStatus.PartiallyApproved, payable, "Approved with adjustments.")
            : new ClaimDecision(claimNumber, ClaimStatus.Approved, payable, "Approved in full.");
    }
}
