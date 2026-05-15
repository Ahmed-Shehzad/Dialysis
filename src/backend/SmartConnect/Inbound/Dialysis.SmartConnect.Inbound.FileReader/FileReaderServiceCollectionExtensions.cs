using Dialysis.SmartConnect.Inbound.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Inbound.FileReader;

/// <summary>DI registration for <see cref="FileReaderSourceConnector"/>.</summary>
public static class FileReaderServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="FileReaderSourceConnector"/> with the source-connector registry.
        /// Operators add instances under <c>SmartConnect:SourceConnectors:Instances</c> with
        /// <c>Kind = "file-reader"</c>.
        /// </summary>
        public IServiceCollection AddSmartConnectFileReader() =>
            services.AddSourceConnector<FileReaderSourceConnector>();
    }
}
