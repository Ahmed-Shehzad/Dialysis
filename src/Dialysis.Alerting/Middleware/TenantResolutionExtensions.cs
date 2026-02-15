namespace Dialysis.Alerting.Middleware;

public static class TenantResolutionExtensions
{
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app, bool requireTenant = false)
    {
        return app.UseMiddleware<TenantResolutionMiddleware>(requireTenant);
    }
}
