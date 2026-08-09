namespace Sample.ClaimsPortal.Claims;

/// <summary>
/// SF0006 hit. <c>Envelope&lt;T&gt;</c> was written "so we can wrap anything", but every
/// construction site in the solution binds T to <see cref="ClaimSubmission" />. A generic
/// parameter with one specialization is indirection without flexibility — the fix is a
/// non-generic <c>ClaimSubmissionEnvelope</c>.
/// </summary>
public sealed record Envelope<T>(T Payload, string CorrelationId, DateTimeOffset ReceivedAt)
    where T : class
{
    public Envelope<T> WithCorrelationId(string correlationId) => this with { CorrelationId = correlationId };
}
