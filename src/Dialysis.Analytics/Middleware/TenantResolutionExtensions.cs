namespace Dialysis.Analytics.Middleware;

public static class TenantResolutionExtensions
{
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app) =>
        app.UseMiddleware<TenantResolutionMiddleware>();
}
