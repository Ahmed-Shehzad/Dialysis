using Dialysis.SmartConnect.Inbound.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Inbound.Sftp;

public static class SftpInboundServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the SFTP source connector (kind <c>sftp</c>). Each
        /// <c>SmartConnect:SourceConnectors:[]</c> entry with <c>Kind=sftp</c> creates one runtime
        /// instance under <see cref="SourceConnectorHostedService"/>.
        /// </summary>
        public IServiceCollection AddSmartConnectSftpInbound() =>
            services.AddSourceConnector<SftpSourceConnector>();
    }
}
