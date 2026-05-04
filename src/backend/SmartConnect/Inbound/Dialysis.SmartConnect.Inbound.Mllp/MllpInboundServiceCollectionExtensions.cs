using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Inbound.Mllp;

/// <summary>Registers MLLP TCP inbound listener.</summary>
public static class MllpInboundServiceCollectionExtensions
{
    /// <summary>
    /// Binds <see cref="MllpInboundOptions"/> from configuration section <c>SmartConnect:Mllp</c> and registers the TCP hosted service.
    /// </summary>
    public static IServiceCollection AddSmartConnectMllpInbound(this IServiceCollection services)
    {
        services.AddOptions<MllpInboundOptions>().BindConfiguration("SmartConnect:Mllp");
        services.AddHostedService<MllpInboundHostedService>();
        return services;
    }
}
