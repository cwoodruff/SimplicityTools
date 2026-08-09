namespace Sample.ClaimsPortal.Notifications;

/// <summary>
/// SF0001 hit: one implementation, <see cref="ConsoleNotifier" />.
/// </summary>
public interface INotifier
{
    void Notify(string recipient, string subject, string body);
}
