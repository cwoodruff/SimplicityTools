namespace Sample.ClaimsPortal.Api.Support;

public sealed record ApiResult(int StatusCode, string Body, RequestContext Context)
{
    public static ApiResult Ok(string body, RequestContext context) => new(200, body, context);

    public static ApiResult Accepted(string body, RequestContext context) => new(202, body, context);

    public static ApiResult NotFound(RequestContext context) => new(404, "Not found.", context);

    public static ApiResult BadRequest(string body, RequestContext context) => new(400, body, context);

    public string Render() => $"{StatusCode} {Body} ({Context.Describe()})";
}
