using Sample.ClaimsPortal.Platform;

namespace Sample.ClaimsPortal.Claims;

public sealed record ClaimSubmission(
    string PolicyNumber,
    ClaimCategory Category,
    Money ClaimedAmount,
    DateOnly IncidentDate,
    string ClaimantEmail,
    string Description,
    int SupportingDocumentCount,
    int ClaimsInLastNinetyDays,
    Money AverageHistoricalClaim);
