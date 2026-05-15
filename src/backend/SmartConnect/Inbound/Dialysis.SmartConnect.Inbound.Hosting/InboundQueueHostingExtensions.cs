using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.SmartConnect.Inbound.Hosting;

/// <summary>Registers the channel-backed queue and <see cref="SmartConnectInboundQueueConsumer"/>.</summary>
public static class InboundQueueHostingExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers a singleton <see cref="ChannelInboundQueue"/> as <see cref="IInboundQueueSubscription"/> and starts <see cref="SmartConnectInboundQueueConsumer"/>.
        /// </summary>
        public IServiceCollection AddSmartConnectChannelInboundQueueConsumer()
        {
            services.TryAddSingleton<ChannelInboundQueue>();
            services.TryAddSingleton<IInboundQueueSubscription>(sp => sp.GetRequiredService<ChannelInboundQueue>());
            services.AddHostedService<SmartConnectInboundQueueConsumer>();
            return services;
        }
    }
}
