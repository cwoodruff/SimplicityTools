namespace Sample.ClaimsPortal.Notifications;

public sealed class ConsoleNotifier : INotifier
{
    private readonly List<string> _sent = [];

    public IReadOnlyList<string> Sent => _sent;

    public void Notify(string recipient, string subject, string body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient);
        _sent.Add($"to:{recipient} subject:{subject} body:{body}");
    }
}
