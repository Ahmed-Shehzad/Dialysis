using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Tenancy;

public static class TenancyExtensions
{
    /// <summary>
    /// Registers tenant resolution services. Use <see cref="UseTenantResolution"/> in the pipeline.
    /// </summary>
    public static IServiceCollection AddTenantResolution(this IServiceCollection services) => services.AddScoped<ITenantContext, TenantContext>();

    /// <summary>
    /// Adds TenantResolutionMiddleware to resolve X-Tenant-Id per request.
    /// Place early in the pipeline, before auth.
    /// </summary>
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app) => app.UseMiddleware<TenantResolutionMiddleware>();
}
