using Sample.ClaimsPortal.Platform;

namespace Sample.ClaimsPortal.Payments;

public sealed record PayoutRequest(string ClaimNumber, string PayeeName, Money Amount);
