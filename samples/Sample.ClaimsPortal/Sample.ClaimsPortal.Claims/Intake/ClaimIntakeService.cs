using Sample.ClaimsPortal.Claims.Journal;
using Sample.ClaimsPortal.Notifications;
using Sample.ClaimsPortal.Payments;
using Sample.ClaimsPortal.Platform;
using Sample.ClaimsPortal.Platform.Identity;
using Sample.ClaimsPortal.Platform.Telemetry;
using Sample.ClaimsPortal.Platform.Time;
using Sample.ClaimsPortal.Policies;

namespace Sample.ClaimsPortal.Claims.Intake;

/// <summary>
/// SF0005 hit: eight constructor parameters, one over the default threshold of seven. A
/// constructor this wide is the signal that one class is doing intake, adjudication routing,
/// payout, notification, and telemetry all at once.
/// </summary>
public sealed class ClaimIntakeService
{
    private readonly IClaimNumberGenerator _claimNumbers;
    private readonly IClock _clock;
    private readonly ClaimWorkflow _workflow;
    private readonly INotifier _notifier;
    private readonly PayoutService _payouts;
    private readonly ITelemetrySink _telemetry;
    private readonly DecisionStore _decisions;
    private readonly IPolicyDirectory _policies;

    public ClaimIntakeService(
        IClaimNumberGenerator claimNumbers,
        IClock clock,
        ClaimWorkflow workflow,
        INotifier notifier,
        PayoutService payouts,
        ITelemetrySink telemetry,
        DecisionStore decisions,
        IPolicyDirectory policies)
    {
        ArgumentNullException.ThrowIfNull(claimNumbers);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(notifier);
        ArgumentNullException.ThrowIfNull(payouts);
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(decisions);
        ArgumentNullException.ThrowIfNull(policies);

        _claimNumbers = claimNumbers;
        _clock = clock;
        _workflow = workflow;
        _notifier = notifier;
        _payouts = payouts;
        _telemetry = telemetry;
        _decisions = decisions;
        _policies = policies;
    }

    public ClaimDecision Intake(ClaimSubmission submission, string correlationId)
    {
        ArgumentNullException.ThrowIfNull(submission);

        var claimNumber = _claimNumbers.Next(submission.Category.ToString().ToUpperInvariant()[..3]);

        // The only place Envelope<T> is ever constructed, and always with ClaimSubmission (SF0006).
        var envelope = new Envelope<ClaimSubmission>(submission, correlationId, _clock.UtcNow);

        var decision = _workflow.Submit(claimNumber, envelope);

        _telemetry.Record("claim.decided", new Dictionary<string, string>
        {
            ["claim"] = decision.ClaimNumber,
            ["status"] = decision.Status.ToString()
        });

        if (decision.IsPayable)
        {
            var payeeName = _policies.Find(submission.PolicyNumber)?.HolderName ?? "Policy holder";
            var payout = _payouts.Settle(new PayoutRequest(decision.ClaimNumber, payeeName, decision.ApprovedAmount));

            _notifier.Notify(
                submission.ClaimantEmail,
                NotificationTemplate.DecisionSubject(decision.ClaimNumber),
                payout.Settled
                    ? NotificationTemplate.ApprovedBody(decision.ClaimNumber, payout.Amount)
                    : NotificationTemplate.DeniedBody(decision.ClaimNumber, payout.Message));

            return decision;
        }

        _notifier.Notify(
            submission.ClaimantEmail,
            NotificationTemplate.DecisionSubject(decision.ClaimNumber),
            decision.Status == ClaimStatus.ManualReview
                ? NotificationTemplate.ManualReviewBody(decision.ClaimNumber)
                : NotificationTemplate.DeniedBody(decision.ClaimNumber, decision.Reason));

        return decision;
    }

    public ClaimDecision? Lookup(string claimNumber) => _decisions.Find(claimNumber);
}
