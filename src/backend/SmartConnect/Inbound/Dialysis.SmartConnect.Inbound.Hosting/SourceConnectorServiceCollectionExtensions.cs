using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.SmartConnect.Inbound.Hosting;

/// <summary>DI registration for the <see cref="ISourceConnectorRegistry"/> and the host.</summary>
public static class SourceConnectorServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="ISourceConnectorRegistry"/> as a singleton, binds
        /// <see cref="SourceConnectorHostOptions"/> from configuration section
        /// <c>SmartConnect:SourceConnectors</c>, and starts <see cref="SourceConnectorHostedService"/>.
        /// </summary>
        public IServiceCollection AddSmartConnectSourceConnectors()
        {
            services.TryAddSingleton<SourceConnectorRegistry>();
            services.TryAddSingleton<ISourceConnectorRegistry>(sp => sp.GetRequiredService<SourceConnectorRegistry>());
            services.AddOptions<SourceConnectorHostOptions>().BindConfiguration("SmartConnect:SourceConnectors");
            services.AddHostedService<SourceConnectorHostedService>();
            return services;
        }
        /// <summary>
        /// Registers <typeparamref name="TConnector"/> as a singleton and adds it to the
        /// <see cref="ISourceConnectorRegistry"/> at startup.
        /// </summary>
        public IServiceCollection AddSourceConnector<TConnector>()
            where TConnector : class, ISourceConnector
        {
            services.AddSmartConnectSourceConnectors();
            services.AddSingleton<TConnector>();
            services.AddSingleton<ISourceConnector>(sp =>
            {
                var connector = sp.GetRequiredService<TConnector>();
                sp.GetRequiredService<SourceConnectorRegistry>().Register(connector);
                return connector;
            });
            return services;
        }
    }
}
