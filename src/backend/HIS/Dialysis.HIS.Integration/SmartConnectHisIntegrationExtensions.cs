using Dialysis.SmartConnect;
using Dialysis.SmartConnect.Inbound;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.HIS.Integration;

/// <summary>
/// Registers SmartConnect runtime primitives for HIS hosts that route clinical or device traffic through integration flows.
/// Also add persistence (<c>AddSmartConnectPersistence*</c>), inbound transports, and Transponder transports per deployment.
/// </summary>
public static class SmartConnectHisIntegrationExtensions
{
    public static IServiceCollection AddSmartConnectForHis(this IServiceCollection services)
    {
        services.AddSmartConnectCore();
        services.AddDefaultInboundMessageFactory();
        services.AddSmartConnectInboundTransport();
        return services;
    }
}
