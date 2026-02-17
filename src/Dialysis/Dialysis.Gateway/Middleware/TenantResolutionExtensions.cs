namespace Dialysis.Gateway.Middleware;

public static class TenantResolutionExtensions
{
    /// <summary>
    /// Gets the current tenant ID from the request context.
    /// </summary>
    public static string GetTenantId(this HttpContext context)
    {
        return (context.Items["TenantId"] as string) ?? TenantResolutionMiddleware.DefaultTenantId;
    }

    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TenantResolutionMiddleware>();
    }
}
