using Sample.ClaimsPortal.Platform;

namespace Sample.ClaimsPortal.Payments;

public sealed record PayoutResult(string ClaimNumber, Money Amount, bool Settled, string Message);
