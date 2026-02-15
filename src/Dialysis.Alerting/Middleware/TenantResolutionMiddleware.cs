using Dialysis.Tenancy;

namespace Dialysis.Alerting.Middleware;

public sealed class TenantResolutionMiddleware
{
    public const string TenantHeaderName = "X-Tenant-Id";
    public const string DefaultTenantId = "default";

    private readonly RequestDelegate _next;
    private readonly bool _requireTenant;

    public TenantResolutionMiddleware(RequestDelegate next, bool requireTenant = false)
    {
        _next = next;
        _requireTenant = requireTenant;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        var tenantId = context.Request.Headers[TenantHeaderName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(tenantId))
            tenantId = DefaultTenantId;
        else
            tenantId = tenantId.Trim().ToLowerInvariant();

        tenantContext.TenantId = tenantId;

        if (_requireTenant && string.IsNullOrWhiteSpace(tenantContext.TenantId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = $"Missing or invalid {TenantHeaderName} header." });
            return;
        }

        await _next(context);
    }
}
