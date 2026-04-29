namespace Sample.OverEngineered.Authorization;

public interface IAuthorizationService
{
    void EnsureCanPlaceOrder(string actorId);
}
