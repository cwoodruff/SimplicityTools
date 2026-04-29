namespace Sample.OverEngineered.Authorization;

public sealed class AuthorizationService : IAuthorizationService
{
    private readonly IRoleResolver _roleResolver;

    public AuthorizationService(IRoleResolver roleResolver)
    {
        _roleResolver = roleResolver;
    }

    public void EnsureCanPlaceOrder(string actorId)
    {
        var role = _roleResolver.ResolveRole(actorId);
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new UnauthorizedAccessException("The actor could not be resolved to a role.");
        }
    }
}
