namespace Sample.OverEngineered.Authorization;

public sealed class RoleResolver : IRoleResolver
{
    public string ResolveRole(string actorId)
    {
        return string.IsNullOrWhiteSpace(actorId) ? string.Empty : "OrderOperator";
    }
}
