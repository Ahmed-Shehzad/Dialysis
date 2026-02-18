using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Tenancy;

/// <summary>
/// Resolves tenant from X-Tenant-Id header and sets it on the scoped TenantContext.
/// C5 multi-tenancy: tenant isolation for all persistence and cache operations.
/// </summary>
public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (tenantContext is TenantContext tc)
        {
            string? headerValue = context.Request.Headers[TenantContext.TenantIdHeader].FirstOrDefault();
            tc.TenantId = string.IsNullOrWhiteSpace(headerValue) ? TenantContext.DefaultTenantId : headerValue.Trim();
        }

        await _next(context);
    }
}
