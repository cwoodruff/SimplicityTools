using Newtonsoft.Json;

namespace Sample.ClaimsPortal.Claims.Journal;

/// <summary>
/// Uses Newtonsoft.Json, so SF0002 correctly leaves that PackageReference alone — the contrast
/// case for the unused Humanizer.Core reference in Sample.ClaimsPortal.Notifications.
/// </summary>
public sealed class DecisionJournal
{
    private readonly JournalWriter _writer;

    public DecisionJournal(JournalWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        _writer = writer;
    }

    public void Append(ClaimDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);

        var payload = JsonConvert.SerializeObject(new
        {
            decision.ClaimNumber,
            Status = decision.Status.ToString(),
            Amount = decision.ApprovedAmount.Amount,
            decision.Reason
        });

        _writer.Write(payload);
    }
}
