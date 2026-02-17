namespace Dialysis.Gateway.Middleware;

/// <summary>
/// Resolves tenant from X-Tenant-Id header. Default is "default" when omitted.
/// </summary>
public sealed class TenantResolutionMiddleware
{
    private const string TenantIdKey = "TenantId";
    internal const string DefaultTenantId = "default";
    private const string HeaderName = "X-Tenant-Id";

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrEmpty(tenantId) && context.User.Identity?.IsAuthenticated == true)
            tenantId = context.User.FindFirst("tenant_id")?.Value ?? context.User.FindFirst("tenantid")?.Value;
        tenantId ??= DefaultTenantId;
        context.Items[TenantIdKey] = tenantId;
        context.Response.Headers[HeaderName] = tenantId;
        await _next(context);
    }
}
