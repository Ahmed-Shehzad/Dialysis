using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Inbound.Mllp;

/// <summary>Registers MLLP TCP inbound listener.</summary>
public static class MllpInboundServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Binds <see cref="MllpInboundOptions"/> from configuration section <c>SmartConnect:Mllp</c> and registers the TCP hosted service.
        /// </summary>
        public IServiceCollection AddSmartConnectMllpInbound()
        {
            services.AddOptions<MllpInboundOptions>().BindConfiguration("SmartConnect:Mllp");
            services.AddHostedService<MllpInboundHostedService>();
            return services;
        }
    }
}
