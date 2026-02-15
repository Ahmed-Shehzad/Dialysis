using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.Tenancy;

public static class TenancyServiceCollectionExtensions
{
    public static IServiceCollection AddTenancy(this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.Configure<TenantConnectionOptions>(configuration.GetSection(TenantConnectionOptions.SectionName));
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<ITenantConnectionResolver, TenantConnectionResolver>();
        return services;
    }
}
