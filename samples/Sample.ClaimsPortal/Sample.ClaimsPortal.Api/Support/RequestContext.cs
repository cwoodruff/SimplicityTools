namespace Sample.ClaimsPortal.Api.Support;

/// <summary>
/// SF0007 hit. Every file in this project touches <see cref="RequestContext" /> — it is referenced
/// more often than any file in the primary-path Endpoints/ folder. When a support type outranks
/// the business flow, readers learn the plumbing before they learn the product.
/// </summary>
public sealed record RequestContext(string CorrelationId, string Caller, DateTimeOffset ReceivedAt)
{
    public static RequestContext ForCaller(string caller) =>
        new(Guid.NewGuid().ToString("N")[..12], caller, DateTimeOffset.UtcNow);

    public RequestContext Rename(string caller) => this with { Caller = caller };

    public string Describe() => $"{Caller}#{CorrelationId}";
}
