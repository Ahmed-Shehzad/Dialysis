namespace Dialysis.HIS.Api.Middleware;

/// <summary>Propagates or assigns <c>X-Correlation-Id</c> for request tracing (ingress may set the header).</summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-Id";

    public Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var incoming) || string.IsNullOrWhiteSpace(incoming))
            context.Response.Headers.Append(HeaderName, Guid.CreateVersion7().ToString("D"));
        else
            context.Response.Headers.Append(HeaderName, incoming.ToString());
        return next(context);
    }
}
