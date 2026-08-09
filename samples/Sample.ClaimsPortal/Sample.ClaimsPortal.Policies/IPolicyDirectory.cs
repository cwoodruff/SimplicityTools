namespace Sample.ClaimsPortal.Policies;

/// <summary>
/// SF0001 hit: one implementation, <see cref="InMemoryPolicyDirectory" />. The team added the
/// interface "for when we move to the policy service", which has not happened.
/// </summary>
public interface IPolicyDirectory
{
    Policy? Find(string policyNumber);
}
