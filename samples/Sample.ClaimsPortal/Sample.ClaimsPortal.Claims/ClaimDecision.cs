using Sample.ClaimsPortal.Platform;

namespace Sample.ClaimsPortal.Claims;

public sealed record ClaimDecision(
    string ClaimNumber,
    ClaimStatus Status,
    Money ApprovedAmount,
    string Reason)
{
    public bool IsPayable => Status is ClaimStatus.Approved or ClaimStatus.PartiallyApproved;

    public static ClaimDecision Rejected(string claimNumber, string reason) =>
        new(claimNumber, ClaimStatus.Denied, Money.Zero, reason);
}
