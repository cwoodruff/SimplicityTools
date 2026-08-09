using Sample.ClaimsPortal.Platform;

namespace Sample.ClaimsPortal.Notifications;

/// <summary>
/// These templates once called Humanizer's Humanize()/ToWords() helpers. They now use
/// interpolated strings, which is why the Humanizer.Core PackageReference in the .csproj is
/// dead weight and SF0002 flags it.
/// </summary>
public static class NotificationTemplate
{
    public static string DecisionSubject(string claimNumber) => $"Claim {claimNumber} decision";

    public static string ApprovedBody(string claimNumber, Money amount) =>
        $"Claim {claimNumber} was approved. Payment of {amount} is on the way.";

    public static string DeniedBody(string claimNumber, string reason) =>
        $"Claim {claimNumber} was denied. Reason: {reason}";

    public static string ManualReviewBody(string claimNumber) =>
        $"Claim {claimNumber} needs a second look. An adjuster will contact you within two business days.";
}
