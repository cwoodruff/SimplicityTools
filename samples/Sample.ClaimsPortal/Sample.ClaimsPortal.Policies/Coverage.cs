using Sample.ClaimsPortal.Platform;

namespace Sample.ClaimsPortal.Policies;

public sealed record Coverage(ClaimCategory Category, Money PerClaimLimit, Money Deductible);
