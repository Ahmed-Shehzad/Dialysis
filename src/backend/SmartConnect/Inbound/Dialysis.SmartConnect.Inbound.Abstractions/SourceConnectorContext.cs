using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dialysis.SmartConnect.Inbound;

/// <summary>
/// Per-connector runtime context provided by the host. Carries the configured flow id,
/// kind-specific parameters, message factory, dispatch callback, and a logger.
/// </summary>
public sealed class SourceConnectorContext
{
    /// <param name="instanceName">Operator-friendly instance label, used in logs.</param>
    /// <param name="defaultFlowId">Target <see cref="IntegrationMessage.FlowId"/> for messages this connector dispatches.</param>
    /// <param name="parameters">Kind-specific configuration (case-insensitive keys).</param>
    /// <param name="messageFactory">Factory used to build <see cref="IntegrationMessage"/> instances.</param>
    /// <param name="dispatchAsync">Callback that resolves a scoped <see cref="IInboundTransport"/> and dispatches the message.</param>
    /// <param name="logger">Logger; may be <see cref="NullLogger.Instance"/>.</param>
    public SourceConnectorContext(
        string instanceName,
        Guid defaultFlowId,
        IReadOnlyDictionary<string, string> parameters,
        IInboundMessageFactory messageFactory,
        Func<IntegrationMessage, CancellationToken, Task<InboundReceiveResult>> dispatchAsync,
        ILogger logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceName);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(messageFactory);
        ArgumentNullException.ThrowIfNull(dispatchAsync);

        InstanceName = instanceName;
        DefaultFlowId = defaultFlowId;
        Parameters = parameters;
        MessageFactory = messageFactory;
        DispatchAsync = dispatchAsync;
        Logger = logger ?? NullLogger.Instance;
    }

    public string InstanceName { get; }

    public Guid DefaultFlowId { get; }

    public IReadOnlyDictionary<string, string> Parameters { get; }

    public IInboundMessageFactory MessageFactory { get; }

    /// <summary>Resolve a scoped <see cref="IInboundTransport"/> and dispatch the message.</summary>
    public Func<IntegrationMessage, CancellationToken, Task<InboundReceiveResult>> DispatchAsync { get; }

    public ILogger Logger { get; }
}
