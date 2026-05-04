using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Inbound.Transponder;

/// <summary>Registers the Transponder transport → SmartConnect channel bridge.</summary>
public static class TransponderInboundServiceCollectionExtensions
{
    /// <summary>Binds <see cref="TransponderInboundBridgeOptions"/> from <c>SmartConnect:TransponderInbound</c>.</summary>
    public static IServiceCollection AddSmartConnectTransponderInboundBridge(this IServiceCollection services)
    {
        services.AddOptions<TransponderInboundBridgeOptions>().BindConfiguration("SmartConnect:TransponderInbound");
        services.AddHostedService<TransponderInboundTransportBridge>();
        return services;
    }
}
