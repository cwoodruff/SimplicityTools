using Sample.ClaimsPortal.Platform;

namespace Sample.ClaimsPortal.Policies;

public sealed class InMemoryPolicyDirectory : IPolicyDirectory
{
    private readonly Dictionary<string, Policy> _policies;

    public InMemoryPolicyDirectory(IEnumerable<Policy> policies)
    {
        ArgumentNullException.ThrowIfNull(policies);
        _policies = policies.ToDictionary(policy => policy.PolicyNumber, StringComparer.OrdinalIgnoreCase);
    }

    public Policy? Find(string policyNumber) =>
        _policies.TryGetValue(policyNumber, out var policy) ? policy : null;

    /// <summary>
    /// Seeds three policies whose coverage windows straddle <paramref name="today" />, so the
    /// demo and the docs keep producing the same decisions no matter when they are run.
    /// </summary>
    public static InMemoryPolicyDirectory CreateSeeded(DateOnly? today = null)
    {
        var anchor = today ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var from = anchor.AddYears(-1);
        var to = anchor.AddYears(1);

        return new InMemoryPolicyDirectory(
        [
            new Policy(
                "AUTO-1001",
                "Dana Reyes",
                from,
                to,
                [new Coverage(ClaimCategory.Auto, new Money(25_000m), new Money(500m))]),
            new Policy(
                "HOME-2002",
                "Ito Nakamura",
                from,
                to,
                [
                    new Coverage(ClaimCategory.Property, new Money(150_000m), new Money(1_000m)),
                    new Coverage(ClaimCategory.Liability, new Money(50_000m), new Money(250m))
                ]),
            new Policy(
                "MED-3003",
                "Priya Anand",
                from,
                to,
                [new Coverage(ClaimCategory.Medical, new Money(75_000m), new Money(2_500m))])
        ]);
    }
}
