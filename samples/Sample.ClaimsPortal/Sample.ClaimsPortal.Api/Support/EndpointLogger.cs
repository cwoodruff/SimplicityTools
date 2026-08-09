namespace Sample.ClaimsPortal.Api.Support;

public sealed class EndpointLogger
{
    private readonly List<string> _entries = [];

    public IReadOnlyList<string> Entries => _entries;

    public void Begin(RequestContext context, string route)
    {
        ArgumentNullException.ThrowIfNull(context);
        _entries.Add($"begin {route} {context.Describe()} at {context.ReceivedAt:O}");
    }

    public void End(RequestContext context, ApiResult result)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(result);
        _entries.Add($"end {context.Describe()} -> {result.StatusCode}");
    }
}
