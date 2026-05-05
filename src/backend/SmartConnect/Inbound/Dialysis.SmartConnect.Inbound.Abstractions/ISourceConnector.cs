namespace Dialysis.SmartConnect.Inbound;

/// <summary>
/// Generalized source-connector contract: a long-running listener/poller that captures inbound
/// payloads from a transport (file, TCP, queue, web service, ...), builds <see cref="IntegrationMessage"/>
/// instances, and dispatches them via <see cref="IInboundTransport"/>.
/// </summary>
/// <remarks>
/// Implementations are typically singletons. The orchestrating
/// <see cref="SourceConnectorContext"/> resolves <see cref="IInboundMessageFactory"/> and a per-dispatch
/// <see cref="IInboundTransport"/> scope so connectors do not need to know about DI scoping rules.
/// </remarks>
public interface ISourceConnector
{
    /// <summary>Stable lookup key (e.g. <c>"file-reader"</c>, <c>"tcp"</c>); case-insensitive.</summary>
    string Kind { get; }

    /// <summary>
    /// Begin listening/polling. Implementations should honour <paramref name="cancellationToken"/>
    /// and return only when the connector has fully shut down.
    /// </summary>
    Task RunAsync(SourceConnectorContext context, CancellationToken cancellationToken);
}
