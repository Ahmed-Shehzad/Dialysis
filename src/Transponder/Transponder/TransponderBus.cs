using Microsoft.Extensions.Logging;

using Transponder.Abstractions;
using Transponder.Persistence;
using Transponder.Transports;
using Transponder.Transports.Abstractions;

namespace Transponder;

/// <summary>
/// Default bus implementation backed by transport hosts.
/// </summary>
public sealed class TransponderBus : IBusControl
{
    private readonly ITransportHostProvider _hostProvider;
    private readonly IReadOnlyCollection<ITransportHost> _hosts;
    private readonly IMessageSerializer _serializer;
    private readonly IMessageScheduler _scheduler;
    private readonly IReadOnlyCollection<IReceiveEndpoint> _receiveEndpoints;
    private readonly Func<Type, Uri?>? _requestAddressResolver;
    private readonly TimeSpan _defaultRequestTimeout;
    private readonly OutboxDispatcher? _outboxDispatcher;
    private readonly IReadOnlyList<ITransponderMessageScopeProvider> _scopeProviders;
    private readonly ILoggerFactory? _loggerFactory;

    public TransponderBus(
        Uri address,
        ITransportHostProvider hostProvider,
        IEnumerable<ITransportHost> hosts,
        IMessageSerializer serializer,
        TransponderBusRuntimeOptions? options = null)
    {
        options ??= new TransponderBusRuntimeOptions();

        Address = address ?? throw new ArgumentNullException(nameof(address));
        _hostProvider = hostProvider ?? throw new ArgumentNullException(nameof(hostProvider));
        _hosts = hosts?.ToArray() ?? throw new ArgumentNullException(nameof(hosts));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _requestAddressResolver = options.RequestAddressResolver;
        _defaultRequestTimeout = options.DefaultRequestTimeout ?? TimeSpan.FromSeconds(30);
        _scheduler = (options.SchedulerFactory ?? (bus => new InMemoryMessageScheduler(bus)))(this);
        _receiveEndpoints = options.ReceiveEndpoints?.ToArray() ?? [];
        _outboxDispatcher = options.OutboxDispatcher;
        _scopeProviders = options.MessageScopeProviders?.ToArray() ?? [];
        _loggerFactory = options.LoggerFactory;
    }

    /// <inheritdoc />
    public Uri Address { get; }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        foreach (var host in _hosts) await host.StartAsync(cancellationToken).ConfigureAwait(false);

        foreach (var endpoint in _receiveEndpoints) await endpoint.StartAsync(cancellationToken).ConfigureAwait(false);

        if (_outboxDispatcher is not null)
            await _outboxDispatcher.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_outboxDispatcher is not null)
            await _outboxDispatcher.StopAsync(cancellationToken).ConfigureAwait(false);

        foreach (var endpoint in _receiveEndpoints) await endpoint.StopAsync(cancellationToken).ConfigureAwait(false);

        foreach (var host in _hosts) await host.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class, IMessage
        => PublishInternalAsync(message, null, cancellationToken);

    /// <inheritdoc />
    public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        ISendEndpoint endpoint = new TransponderSendEndpoint(this, address);
        return Task.FromResult(endpoint);
    }

    /// <inheritdoc />
    public IRequestClient<TRequest> CreateRequestClient<TRequest>(TimeSpan? timeout = null)
        where TRequest : class, IMessage
    {
        var resolver = _requestAddressResolver
                       ?? throw new InvalidOperationException("Request address resolver is not configured.");

        var address = resolver(typeof(TRequest))
                      ?? throw new InvalidOperationException($"No request address configured for {typeof(TRequest).Name}.");

        var requestTimeout = timeout ?? _defaultRequestTimeout;

        var logger = _loggerFactory?.CreateLogger<RequestClient<TRequest>>();
        
        return new RequestClient<TRequest>(
            this,
            _serializer,
            _hostProvider,
            address,
            requestTimeout,
            logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<RequestClient<TRequest>>.Instance);
    }

    /// <inheritdoc />
    public Task<IScheduledMessageHandle> ScheduleSendAsync<TMessage>(
        Uri destinationAddress,
        TMessage message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken = default)
        where TMessage : class, IMessage
        => _scheduler.ScheduleSendAsync(destinationAddress, message, scheduledTime, cancellationToken);

    /// <inheritdoc />
    public Task<IScheduledMessageHandle> ScheduleSendAsync<TMessage>(
        Uri destinationAddress,
        TMessage message,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
        where TMessage : class, IMessage
        => _scheduler.ScheduleSendAsync(destinationAddress, message, delay, cancellationToken);

    /// <inheritdoc />
    public Task<IScheduledMessageHandle> SchedulePublishAsync<TMessage>(
        TMessage message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken = default)
        where TMessage : class, IMessage
        => _scheduler.SchedulePublishAsync(message, scheduledTime, cancellationToken);

    /// <inheritdoc />
    public Task<IScheduledMessageHandle> SchedulePublishAsync<TMessage>(
        TMessage message,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
        where TMessage : class, IMessage
        => _scheduler.SchedulePublishAsync(message, delay, cancellationToken);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);

        if (_scheduler is IAsyncDisposable asyncDisposable) await asyncDisposable.DisposeAsync().ConfigureAwait(false);

        await DisposeHostsAsync().ConfigureAwait(false);

        foreach (var endpoint in _receiveEndpoints) await endpoint.DisposeAsync().ConfigureAwait(false);

        if (_outboxDispatcher is not null) await _outboxDispatcher.DisposeAsync().ConfigureAwait(false);
    }

    private async Task DisposeHostsAsync()
    {
        foreach (var host in _hosts) await host.DisposeAsync().ConfigureAwait(false);
    }

    internal async Task SendInternalAsync<TMessage>(
        Uri address,
        TMessage message,
        Ulid? messageId,
        Ulid? correlationId,
        Ulid? conversationId,
        IReadOnlyDictionary<string, object?>? headers,
        CancellationToken cancellationToken)
        where TMessage : class, IMessage
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(message);

        if (!correlationId.HasValue && message is ICorrelatedMessage correlatedMessage)
            correlationId = correlatedMessage.CorrelationId;

        if (_outboxDispatcher is not null)
        {
            var outboxMessage = OutboxMessageFactory.Create(
                message,
                _serializer,
                new OutboxMessageFactoryOptions
                {
                    MessageId = messageId,
                    CorrelationId = correlationId,
                    ConversationId = conversationId,
                    SourceAddress = Address,
                    DestinationAddress = address,
                    Headers = headers
                });

            var context = TransponderMessageContextFactory.FromOutboxMessage(outboxMessage);
            using var scope = BeginSendScope(context);

            await _outboxDispatcher.EnqueueAsync(outboxMessage, cancellationToken).ConfigureAwait(false);
            return;
        }

        var host = _hostProvider.GetHost(address);
        var transport = await host.GetSendTransportAsync(address, cancellationToken).ConfigureAwait(false);
        var transportMessage = TransportMessageFactory.Create(
            message,
            _serializer,
            messageId,
            correlationId,
            conversationId,
            headers);

        var messageContext = TransponderMessageContextFactory.FromTransportMessage(
            transportMessage,
            Address,
            address);
        using var sendScope = BeginSendScope(messageContext);

        await transport.SendAsync(transportMessage, cancellationToken).ConfigureAwait(false);
    }

    internal async Task SendObjectAsync(
        Uri address,
        object message,
        IReadOnlyDictionary<string, object?>? headers,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(message);

        var host = _hostProvider.GetHost(address);
        var transport = await host.GetSendTransportAsync(address, cancellationToken).ConfigureAwait(false);
        Ulid? correlationId = message is ICorrelatedMessage correlatedMessage ? correlatedMessage.CorrelationId : null;
        var transportMessage = TransportMessageFactory.Create(
            message,
            _serializer,
            correlationId: correlationId,
            headers: headers);

        var messageContext = TransponderMessageContextFactory.FromTransportMessage(
            transportMessage,
            Address,
            address);
        using var scope = BeginSendScope(messageContext);

        await transport.SendAsync(transportMessage, cancellationToken).ConfigureAwait(false);
    }

    internal Task PublishInternalAsync<TMessage>(
        TMessage message,
        IReadOnlyDictionary<string, object?>? headers,
        CancellationToken cancellationToken)
        where TMessage : class, IMessage
        => PublishInternalAsync(message, null, null, headers, cancellationToken);

    internal async Task PublishInternalAsync<TMessage>(
        TMessage message,
        Ulid? correlationId,
        Ulid? conversationId,
        IReadOnlyDictionary<string, object?>? headers,
        CancellationToken cancellationToken)
        where TMessage : class, IMessage
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!correlationId.HasValue && message is ICorrelatedMessage correlatedMessage)
            correlationId = correlatedMessage.CorrelationId;

        if (_outboxDispatcher is not null)
        {
            var outboxMessage = OutboxMessageFactory.Create(
                message,
                _serializer,
                new OutboxMessageFactoryOptions
                {
                    CorrelationId = correlationId,
                    ConversationId = conversationId,
                    SourceAddress = Address,
                    Headers = headers
                });

            var context = TransponderMessageContextFactory.FromOutboxMessage(outboxMessage);
            using var scope = BeginPublishScope(context);

            await _outboxDispatcher.EnqueueAsync(outboxMessage, cancellationToken).ConfigureAwait(false);
            return;
        }

        var host = _hostProvider.GetHost(Address);
        var transport = await host.GetPublishTransportAsync(message.GetType(), cancellationToken)
            .ConfigureAwait(false);
        var transportMessage = TransportMessageFactory.Create(
            message,
            _serializer,
            correlationId: correlationId,
            conversationId: conversationId,
            headers: headers);

        var messageContext = TransponderMessageContextFactory.FromTransportMessage(
            transportMessage,
            Address,
            destinationAddress: null);
        using var publishScope = BeginPublishScope(messageContext);

        await transport.PublishAsync(transportMessage, cancellationToken).ConfigureAwait(false);
    }

    internal Task PublishObjectAsync(
        object message,
        IReadOnlyDictionary<string, object?>? headers,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var host = _hostProvider.GetHost(Address);
        return PublishObjectAsync(host, message, headers, cancellationToken);
    }

    private async Task PublishObjectAsync(
        ITransportHost host,
        object message,
        IReadOnlyDictionary<string, object?>? headers,
        CancellationToken cancellationToken)
    {
        var messageType = message.GetType();
        var transport = await host.GetPublishTransportAsync(messageType, cancellationToken).ConfigureAwait(false);
        Ulid? correlationId = message is ICorrelatedMessage correlatedMessage ? correlatedMessage.CorrelationId : null;
        var transportMessage = TransportMessageFactory.Create(
            message,
            _serializer,
            correlationId: correlationId,
            headers: headers);

        var messageContext = TransponderMessageContextFactory.FromTransportMessage(
            transportMessage,
            Address,
            destinationAddress: null);
        using var scope = BeginPublishScope(messageContext);

        await transport.PublishAsync(transportMessage, cancellationToken).ConfigureAwait(false);
    }

    internal Uri CreateResponseAddress()
    {
        var builder = new UriBuilder(Address);
        var basePath = builder.Path?.TrimEnd('/') ?? string.Empty;
        builder.Path = $"{basePath}/responses/{Ulid.NewUlid()}";
        return builder.Uri;
    }

    internal IDisposable? BeginConsumeScope(TransponderMessageContext context)
        => TransponderMessageScopeFactory.BeginConsume(_scopeProviders, context);

    private IDisposable? BeginSendScope(TransponderMessageContext context)
        => TransponderMessageScopeFactory.BeginSend(_scopeProviders, context);

    private IDisposable? BeginPublishScope(TransponderMessageContext context)
        => TransponderMessageScopeFactory.BeginPublish(_scopeProviders, context);
}
