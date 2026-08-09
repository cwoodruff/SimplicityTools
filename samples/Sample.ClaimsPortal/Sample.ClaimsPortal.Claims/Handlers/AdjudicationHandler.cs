using Sample.ClaimsPortal.Claims.Adjudication;
using Sample.ClaimsPortal.Fraud;
using Sample.ClaimsPortal.Platform.Time;
using Sample.ClaimsPortal.Policies;

namespace Sample.ClaimsPortal.Claims.Handlers;

/// <summary>
/// Lives in a Handlers/ folder, so both the CLI metrics collector and SF0007 count it as
/// primary-path code.
/// </summary>
public sealed class AdjudicationHandler
{
    private readonly ClaimAdjudicator _adjudicator;
    private readonly IPolicyDirectory _policies;
    private readonly FraudScreener _fraudScreener;
    private readonly IClock _clock;
    private readonly DecisionMapper _mapper;

    public AdjudicationHandler(
        ClaimAdjudicator adjudicator,
        IPolicyDirectory policies,
        FraudScreener fraudScreener,
        IClock clock,
        DecisionMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(adjudicator);
        ArgumentNullException.ThrowIfNull(policies);
        ArgumentNullException.ThrowIfNull(fraudScreener);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(mapper);

        _adjudicator = adjudicator;
        _policies = policies;
        _fraudScreener = fraudScreener;
        _clock = clock;
        _mapper = mapper;
    }

    public ClaimDecision Handle(AdjudicationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var submission = request.Submission;
        var policy = _policies.Find(submission.PolicyNumber);
        var fraudScore = _fraudScreener.Screen(new FraudContext(
            submission.PolicyNumber,
            submission.Category,
            submission.ClaimedAmount,
            submission.ClaimsInLastNinetyDays,
            submission.AverageHistoricalClaim));

        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var decision = _adjudicator.Adjudicate(request.ClaimNumber, submission, policy, fraudScore, today);

        return _mapper.Map(decision);
    }
}
