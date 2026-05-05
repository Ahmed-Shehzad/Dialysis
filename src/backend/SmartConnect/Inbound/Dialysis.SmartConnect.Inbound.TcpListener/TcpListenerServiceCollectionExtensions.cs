using Dialysis.SmartConnect.Inbound.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Inbound.TcpListener;

/// <summary>DI registration for <see cref="TcpListenerSourceConnector"/>.</summary>
public static class TcpListenerServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="TcpListenerSourceConnector"/> with the source-connector registry.
    /// Operators add instances under <c>SmartConnect:SourceConnectors:Instances</c> with
    /// <c>Kind = "tcp-listener"</c>.
    /// </summary>
    public static IServiceCollection AddSmartConnectTcpListener(this IServiceCollection services) =>
        services.AddSourceConnector<TcpListenerSourceConnector>();
}
