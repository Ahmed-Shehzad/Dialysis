using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Inbound.AspNetCore;

/// <summary>Registers HTTP inbound options for SmartConnect.</summary>
public static class SmartConnectInboundAspNetCoreExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="SmartConnectInboundHttpOptions"/> (optional API key, body limit).
        /// </summary>
        public IServiceCollection AddSmartConnectInboundHttpOptions(
            Action<SmartConnectInboundHttpOptions>? configure = null)
        {
            if (configure is not null)
                services.Configure(configure);
            return services;
        }
    }
}
