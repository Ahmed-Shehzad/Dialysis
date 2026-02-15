using BuildingBlocks.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Transponder.Abstractions;
using Transponder.Transports.Abstractions;

namespace Dialysis.Messaging;

/// <summary>
/// Transponder receive endpoint that deserializes integration events and dispatches to <see cref="IIntegrationEventHandler{T}"/>.
/// </summary>
internal sealed class IntegrationEventConsumerEndpoint<TMessage> : IReceiveEndpoint
    where TMessage : class, IIntegrationEvent
{
    private readonly Uri _inputAddress;
    private readonly ITransportHostProvider _hostProvider;
    private readonly IMessageSerializer _serializer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IntegrationEventConsumerEndpoint<TMessage>> _logger;
    private IReceiveEndpoint? _inner;

    public IntegrationEventConsumerEndpoint(
        IOptions<IntegrationEventConsumerOptions<TMessage>> options,
        ITransportHostProvider hostProvider,
        IMessageSerializer serializer,
        IServiceScopeFactory scopeFactory,
        ILogger<IntegrationEventConsumerEndpoint<TMessage>> logger)
    {
        _inputAddress = options.Value.InputAddress
            ?? throw new InvalidOperationException($"InputAddress is required for {typeof(TMessage).Name} consumer.");
        _hostProvider = hostProvider ?? throw new ArgumentNullException(nameof(hostProvider));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Uri InputAddress => _inputAddress;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var host = _hostProvider.GetHost(_inputAddress);
        var configuration = new ReceiveEndpointConfig(_inputAddress, HandleAsync);

        _inner = host.ConnectReceiveEndpoint(configuration);
        await _inner.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_inner is not null)
        {
            await _inner.StopAsync(cancellationToken).ConfigureAwait(false);
            await _inner.DisposeAsync().ConfigureAwait(false);
            _inner = null;
        }
    }

    public ValueTask DisposeAsync() => new(StopAsync());

    private async Task HandleAsync(IReceiveContext context)
    {
        var transportMessage = context.Message;
        var messageTypeName = transportMessage.MessageType;

        if (string.IsNullOrWhiteSpace(messageTypeName))
        {
            messageTypeName = typeof(TMessage).FullName;
        }

        var messageType = Type.GetType(messageTypeName!, throwOnError: false)
                          ?? typeof(TMessage);

        var obj = _serializer.Deserialize(transportMessage.Body.Span, messageType);
        if (obj is not TMessage message)
        {
            _logger.LogWarning(
                "IntegrationEventConsumerEndpoint deserialized to {ActualType}, expected {ExpectedType}",
                obj?.GetType().FullName ?? "null",
                typeof(TMessage).FullName);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetService<IIntegrationEventHandler<TMessage>>();
        if (handler is null)
        {
            _logger.LogWarning(
                "No IIntegrationEventHandler<{MessageType}> registered",
                typeof(TMessage).Name);
            return;
        }

        await handler.HandleAsync(message, context.CancellationToken).ConfigureAwait(false);
    }

    private sealed class ReceiveEndpointConfig : IReceiveEndpointConfiguration
    {
        public ReceiveEndpointConfig(Uri inputAddress, Func<IReceiveContext, Task> handler)
        {
            InputAddress = inputAddress;
            Handler = handler;
            Settings = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        public Uri InputAddress { get; }
        public Func<IReceiveContext, Task> Handler { get; }
        public IReadOnlyDictionary<string, object?> Settings { get; }
    }
}
