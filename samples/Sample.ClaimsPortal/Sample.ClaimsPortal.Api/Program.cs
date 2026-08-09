using Sample.ClaimsPortal.Api.Composition;
using Sample.ClaimsPortal.Api.Support;
using Sample.ClaimsPortal.Claims;
using Sample.ClaimsPortal.Platform;

var host = new ApiHost();

var submissions = new (string Label, ClaimSubmission Submission)[]
{
    ("clean auto claim", new ClaimSubmission(
        "AUTO-1001",
        ClaimCategory.Auto,
        new Money(4_200m),
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
        "dana@example.test",
        "Rear-ended at a stop light.",
        SupportingDocumentCount: 3,
        ClaimsInLastNinetyDays: 0,
        AverageHistoricalClaim: new Money(3_000m))),
    ("thin documentation", new ClaimSubmission(
        "HOME-2002",
        ClaimCategory.Property,
        new Money(18_500m),
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-45)),
        "ito@example.test",
        "Storm damage to the roof.",
        SupportingDocumentCount: 1,
        ClaimsInLastNinetyDays: 1,
        AverageHistoricalClaim: new Money(6_000m))),
    ("fraud screen trips", new ClaimSubmission(
        "MED-3003",
        ClaimCategory.Medical,
        new Money(60_000m),
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20)),
        "priya@example.test",
        "Emergency surgery.",
        SupportingDocumentCount: 4,
        ClaimsInLastNinetyDays: 4,
        AverageHistoricalClaim: new Money(2_500m)))
};

Console.WriteLine("Sample.ClaimsPortal — claim intake walkthrough");
Console.WriteLine(new string('-', 48));

foreach (var (label, submission) in submissions)
{
    RequestContext context = host.NewRequestContext(label);
    var result = host.Claims.Post(submission, context);
    Console.WriteLine($"{label,-22} {result.Render()}");

    var payout = host.Payouts.Get(result.Body.Split(' ')[0], context.Rename($"{label} payout"));
    Console.WriteLine($"{"",-22} {payout.Render()}");
}

Console.WriteLine();
Console.WriteLine($"Journal entries: {host.JournalWriter.Lines.Count}");
Console.WriteLine($"Notifications sent: {host.Notifier.Sent.Count}");
Console.WriteLine($"Telemetry events: {host.Telemetry.Recorded.Count}");
Console.WriteLine($"Endpoint log lines: {host.Logger.Entries.Count}");
