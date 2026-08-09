using Sample.ClaimsPortal.Api.Endpoints;
using Sample.ClaimsPortal.Api.Support;
using Sample.ClaimsPortal.Claims;
using Sample.ClaimsPortal.Claims.Adjudication;
using Sample.ClaimsPortal.Claims.Handlers;
using Sample.ClaimsPortal.Claims.Intake;
using Sample.ClaimsPortal.Claims.Journal;
using Sample.ClaimsPortal.Fraud;
using Sample.ClaimsPortal.Notifications;
using Sample.ClaimsPortal.Payments;
using Sample.ClaimsPortal.Platform.Identity;
using Sample.ClaimsPortal.Platform.Telemetry;
using Sample.ClaimsPortal.Platform.Time;
using Sample.ClaimsPortal.Policies;

namespace Sample.ClaimsPortal.Api.Composition;

/// <summary>
/// Poor-man's composition root. Building the object graph by hand is what makes the ten-frame
/// intake chain impossible to miss.
/// </summary>
public sealed class ApiHost
{
    public ApiHost(IClock? clock = null)
    {
        Clock = clock ?? new SystemClock();
        Telemetry = new ConsoleTelemetrySink();
        Notifier = new ConsoleNotifier();
        Logger = new EndpointLogger();
        Policies = InMemoryPolicyDirectory.CreateSeeded(DateOnly.FromDateTime(Clock.UtcNow.UtcDateTime));
        Ledger = new InMemoryPayoutLedger();
        JournalWriter = new JournalWriter();

        var journal = new DecisionJournal(JournalWriter);
        Decisions = new DecisionStore(journal);

        var mapper = new DecisionMapper(Decisions);
        var handler = new AdjudicationHandler(
            new ClaimAdjudicator(),
            Policies,
            FraudScreener.CreateDefault(),
            Clock,
            mapper);
        var workflow = new ClaimWorkflow(new IntakeCoordinator(new SubmissionValidator(new TriageRouter(new AdjudicationGateway(handler)))));

        var intake = new ClaimIntakeService(
            new SequentialClaimNumberGenerator(),
            Clock,
            workflow,
            Notifier,
            new PayoutService(Ledger, Telemetry),
            Telemetry,
            Decisions,
            Policies);

        Claims = new ClaimsEndpoint(intake, Logger);
        Payouts = new PayoutEndpoint(Ledger, Logger);
    }

    public IClock Clock { get; }

    public ConsoleTelemetrySink Telemetry { get; }

    public ConsoleNotifier Notifier { get; }

    public EndpointLogger Logger { get; }

    public IPolicyDirectory Policies { get; }

    public IPayoutLedger Ledger { get; }

    public JournalWriter JournalWriter { get; }

    public DecisionStore Decisions { get; }

    public ClaimsEndpoint Claims { get; }

    public PayoutEndpoint Payouts { get; }

    public RequestContext NewRequestContext(string caller) => RequestContext.ForCaller(caller);
}
