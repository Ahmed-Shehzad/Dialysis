using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Inbound.Transponder;

/// <summary>Registers the Transponder transport → SmartConnect channel bridge.</summary>
public static class TransponderInboundServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Binds <see cref="TransponderInboundBridgeOptions"/> from <c>SmartConnect:TransponderInbound</c>.</summary>
        public IServiceCollection AddSmartConnectTransponderInboundBridge()
        {
            services.AddOptions<TransponderInboundBridgeOptions>().BindConfiguration("SmartConnect:TransponderInbound");
            services.AddHostedService<TransponderInboundTransportBridge>();
            return services;
        }
        /// <summary>
        /// Same as <see cref="AddSmartConnectTransponderInboundBridge"/> but gated on
        /// <c>SmartConnect:Inbound:TransponderBridge:Enabled</c> being true. No-op otherwise.
        /// </summary>
        public IServiceCollection AddSmartConnectTransponderInboundBridgeIfEnabled(IConfiguration configuration)
        {
            var enabled = configuration.GetValue<bool>("SmartConnect:Inbound:TransponderBridge:Enabled");
            return enabled ? services.AddSmartConnectTransponderInboundBridge() : services;
        }
    }
}
