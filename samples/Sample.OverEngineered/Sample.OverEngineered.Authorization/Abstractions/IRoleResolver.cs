namespace Sample.OverEngineered.Authorization;

public interface IRoleResolver
{
    string ResolveRole(string actorId);
}
