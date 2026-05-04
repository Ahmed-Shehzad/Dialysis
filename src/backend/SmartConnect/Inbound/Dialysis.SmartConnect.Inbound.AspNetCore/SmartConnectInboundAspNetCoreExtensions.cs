using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Inbound.AspNetCore;

/// <summary>Registers HTTP inbound options for SmartConnect.</summary>
public static class SmartConnectInboundAspNetCoreExtensions
{
    /// <summary>
    /// Registers <see cref="SmartConnectInboundHttpOptions"/> (optional API key, body limit).
    /// </summary>
    public static IServiceCollection AddSmartConnectInboundHttpOptions(
        this IServiceCollection services,
        Action<SmartConnectInboundHttpOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);
        return services;
    }
}
