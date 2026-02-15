using Dialysis.Tenancy;

namespace Dialysis.Registry.Middleware;

public sealed class TenantResolutionMiddleware
{
    public const string TenantHeaderName = "X-Tenant-Id";
    public const string DefaultTenantId = "default";

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        var tenantId = context.Request.Headers[TenantHeaderName].FirstOrDefault();
        tenantContext.TenantId = string.IsNullOrWhiteSpace(tenantId) ? DefaultTenantId : tenantId.Trim().ToLowerInvariant();
        await _next(context);
    }
}
