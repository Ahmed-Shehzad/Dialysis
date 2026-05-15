using Dialysis.SmartConnect.Inbound.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Inbound.DatabaseReader;

/// <summary>DI registration for <see cref="DatabaseReaderSourceConnector"/>.</summary>
public static class DatabaseReaderServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="DatabaseReaderSourceConnector"/> with the source-connector registry.
        /// Operators add instances under <c>SmartConnect:SourceConnectors:Instances</c> with
        /// <c>Kind = "database-reader"</c>.
        /// </summary>
        public IServiceCollection AddSmartConnectDatabaseReader() =>
            services.AddSourceConnector<DatabaseReaderSourceConnector>();
    }
}
