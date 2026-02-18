using System.Collections.Concurrent;
using System.Threading.Channels;

using Microsoft.Extensions.Logging;

using Transponder.Persistence;
using Transponder.Persistence.Abstractions;
using Transponder.Transports;
using Transponder.Transports.Abstractions;

namespace Transponder;

public sealed class OutboxDispatcher : IAsyncDisposable
{
    private readonly IStorageSessionFactory _sessionFactory;
    private readonly ITransportHostProvider _hostProvider;
    private readonly OutboxDispatchOptions _options;
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly Uri? _deadLetterAddress;
    private readonly Channel<OutboxMessage> _channel;
    private readonly ConcurrentDictionary<Ulid, byte> _inflight = new();
    private readonly ConcurrentDictionary<string, byte> _activeDestinations = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _dispatchSlots;
    private CancellationTokenSource? _cts;
    private Task? _dispatchLoop;
    private Task? _pollLoop;

    public OutboxDispatcher(
        IStorageSessionFactory sessionFactory,
        ITransportHostProvider hostProvider,
        OutboxDispatchOptions options,
        ILogger<OutboxDispatcher> logger)
    {
        _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        _hostProvider = hostProvider ?? throw new ArgumentNullException(nameof(hostProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _deadLetterAddress = options.DeadLetterAddress;

        if (_options.ChannelCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(options));
        if (_options.BatchSize <= 0) throw new ArgumentOutOfRangeException(nameof(options));
        if (_options.MaxConcurrentDestinations <= 0) throw new ArgumentOutOfRangeException(nameof(options));

        var channelOptions = new BoundedChannelOptions(_options.ChannelCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<OutboxMessage>(channelOptions);
        _dispatchSlots = new SemaphoreSlim(_options.MaxConcurrentDestinations);
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is not null) return Task.CompletedTask;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _dispatchLoop = Task.Run(() => DispatchLoopAsync(_cts.Token), _cts.Token);
        _pollLoop = Task.Run(() => PollLoopAsync(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is null) return;

        await _cts.CancelAsync().ConfigureAwait(false);
        _ = _channel.Writer.TryComplete();

        Task? dispatchLoop = _dispatchLoop;
        Task? pollLoop = _pollLoop;
        _dispatchLoop = null;
        _pollLoop = null;

        if (dispatchLoop is not null || pollLoop is not null)
        {
            var all = Task.WhenAll(dispatchLoop ?? Task.CompletedTask, pollLoop ?? Task.CompletedTask);
            var timeout = Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            Task completed = await Task.WhenAny(all, timeout).ConfigureAwait(false);

            if (completed == timeout) _logger.LogWarning("OutboxDispatcher stop timed out after 30 seconds");
        }

        _activeDestinations.Clear();
        _inflight.Clear();

        _cts.Dispose();
        _cts = null;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _dispatchSlots.Dispose();
    }

    public async Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await using IStorageSession session = await _sessionFactory.CreateSessionAsync(cancellationToken)
            .ConfigureAwait(false);
        await session.Outbox.AddAsync(message, cancellationToken).ConfigureAwait(false);
        await session.CommitAsync(cancellationToken).ConfigureAwait(false);

        TryQueue(message);
    }

    private void TryQueue(OutboxMessage message)
    {
        if (!_inflight.TryAdd(message.MessageId, 0)) return;

        if (_channel.Writer.TryWrite(message)) return;

        _ = _inflight.TryRemove(message.MessageId, out _);
    }

    private async Task DispatchLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                while (_channel.Reader.TryRead(out OutboxMessage? message))
                    await ProcessOneMessageFromChannelAsync(message, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected when cancellation is requested
        }
    }

    private async Task ProcessOneMessageFromChannelAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (!TryGetDestinationKey(message, out string? destinationKey) || !_activeDestinations.TryAdd(destinationKey, 0))
        {
            _ = _inflight.TryRemove(message.MessageId, out _);
            return;
        }

        await _dispatchSlots.WaitAsync(cancellationToken).ConfigureAwait(false);
        _ = Task.Run(() => ProcessMessageAsync(message, destinationKey, cancellationToken), cancellationToken);
    }

    private async Task PollLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await PollOnceAsync(cancellationToken).ConfigureAwait(false);

            using var timer = new PeriodicTimer(_options.PollInterval);

            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                await PollOnceAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected when cancellation is requested
        }
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        await using IStorageSession session = await _sessionFactory.CreateSessionAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<IOutboxMessage> pending = await session.Outbox
            .GetPendingAsync(_options.BatchSize, cancellationToken)
            .ConfigureAwait(false);

        foreach (IOutboxMessage message in pending)
            TryQueue(OutboxMessage.FromMessage(message));

        await session.RollbackAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessMessageAsync(
        OutboxMessage message,
        string destinationKey,
        CancellationToken cancellationToken)
    {
        string destination = message.DestinationAddress?.ToString()
            ?? $"publish:{message.MessageType ?? "unknown"}";

        try
        {
            int attempt = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                (bool done, int nextAttempt) = await TryDispatchAttemptAsync(message, destination, attempt, cancellationToken).ConfigureAwait(false);
                if (done) return;

                attempt = nextAttempt;
                await Task.Delay(_options.RetryDelay, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _ = _activeDestinations.TryRemove(destinationKey, out _);
            _ = _inflight.TryRemove(message.MessageId, out _);
            _ = _dispatchSlots.Release();
        }
    }

    private async Task<(bool done, int nextAttempt)> TryDispatchAttemptAsync(
        OutboxMessage message,
        string destination,
        int attempt,
        CancellationToken cancellationToken)
    {
        attempt++;
        _logger.LogDebug(
            "OutboxDispatcher dispatching message. MessageId={MessageId}, Destination={Destination}, MessageType={MessageType}, Attempt={Attempt}",
            message.MessageId,
            destination,
            message.MessageType ?? "unknown",
            attempt);

        try
        {
            await DispatchMessageAsync(message, cancellationToken).ConfigureAwait(false);
            await MarkSentAsync(message.MessageId, cancellationToken).ConfigureAwait(false);

            if (attempt > 1)
                _logger.LogInformation(
                    "OutboxDispatcher successfully dispatched message after {Attempt} attempts. MessageId={MessageId}, Destination={Destination}",
                    attempt,
                    message.MessageId,
                    destination);

            return (true, attempt);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(ex, "OutboxDispatcher dispatch cancelled. MessageId={MessageId}", message.MessageId);
            return (true, attempt);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "OutboxDispatcher dispatch failed. MessageId={MessageId}, Destination={Destination}, MessageType={MessageType}, Attempt={Attempt}, RetryDelay={RetryDelay}ms",
                message.MessageId,
                destination,
                message.MessageType ?? "unknown",
                attempt,
                _options.RetryDelay.TotalMilliseconds);
            return (false, attempt);
        }
    }

    private async Task MarkSentAsync(Ulid messageId, CancellationToken cancellationToken)
    {
        await using IStorageSession session = await _sessionFactory.CreateSessionAsync(cancellationToken)
            .ConfigureAwait(false);
        await session.Outbox.MarkSentAsync(messageId, DateTimeOffset.UtcNow, cancellationToken)
            .ConfigureAwait(false);
        await session.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task DispatchMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var transportMessage = new TransportMessage(
            message.Body,
            message.ContentType,
            message.Headers,
            new MessageIdentity(message.MessageId, message.CorrelationId, message.ConversationId),
            message.MessageType,
            DateTimeOffset.UtcNow);

        if (message.DestinationAddress is not null)
        {
            await SendToDestinationAsync(transportMessage, message.DestinationAddress, cancellationToken).ConfigureAwait(false);
            return;
        }

        Type? messageType = OutboxMessageTypeResolver.Resolve(message.MessageType);
        if (messageType is null)
        {
            _logger.LogError(
                "OutboxDispatcher: Failed to resolve message type. MessageId={MessageId}, MessageType={MessageType}",
                message.MessageId,
                message.MessageType ?? "null");
            await SendToDeadLetterOrThrowAsync(transportMessage, "UnresolvableMessageType",
                $"Message type '{message.MessageType}' could not be resolved.",
                $"Outbox message type '{message.MessageType}' could not be resolved.",
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (message.SourceAddress is null)
        {
            _logger.LogError(
                "OutboxDispatcher: Source address is required for publish. MessageId={MessageId}, MessageType={MessageType}",
                message.MessageId,
                message.MessageType ?? "unknown");
            await SendToDeadLetterOrThrowAsync(transportMessage, "MissingSourceAddress",
                "Outbox publish requires a source address.",
                "Outbox publish requires a source address.",
                cancellationToken).ConfigureAwait(false);
            return;
        }

        await PublishAsync(transportMessage, messageType, message.SourceAddress, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendToDestinationAsync(
        ITransportMessage transportMessage,
        Uri destinationAddress,
        CancellationToken cancellationToken)
    {
        ITransportHost host = _hostProvider.GetHost(destinationAddress);
        ISendTransport transport = await host.GetSendTransportAsync(destinationAddress, cancellationToken)
            .ConfigureAwait(false);
        await transport.SendAsync(transportMessage, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendToDeadLetterOrThrowAsync(
        ITransportMessage transportMessage,
        string reason,
        string description,
        string throwMessage,
        CancellationToken cancellationToken)
    {
        if (_deadLetterAddress is null)
            throw new InvalidOperationException(throwMessage);

        await SendToDeadLetterQueueAsync(transportMessage, reason, description, cancellationToken).ConfigureAwait(false);
    }

    private async Task PublishAsync(
        ITransportMessage transportMessage,
        Type messageType,
        Uri sourceAddress,
        CancellationToken cancellationToken)
    {
        ITransportHost publishHost = _hostProvider.GetHost(sourceAddress);
        IPublishTransport publishTransport = await publishHost.GetPublishTransportAsync(messageType, cancellationToken)
            .ConfigureAwait(false);
        await publishTransport.PublishAsync(transportMessage, cancellationToken).ConfigureAwait(false);
    }

    private static bool TryGetDestinationKey(OutboxMessage message, out string destinationKey)
    {
        if (message.DestinationAddress is not null)
        {
            destinationKey = message.DestinationAddress.ToString();
            return true;
        }

        if (!string.IsNullOrWhiteSpace(message.MessageType))
        {
            destinationKey = $"publish:{message.MessageType}";
            return true;
        }

        destinationKey = string.Empty;
        return false;
    }

    private async Task SendToDeadLetterQueueAsync(
        ITransportMessage message,
        string reason,
        string description,
        CancellationToken cancellationToken)
    {
        if (_deadLetterAddress is null) return;

        ITransportHost host = _hostProvider.GetHost(_deadLetterAddress);
        ISendTransport transport = await host.GetSendTransportAsync(_deadLetterAddress, cancellationToken)
            .ConfigureAwait(false);

        var deadLetterMessage = new TransportMessage(
            message.Body,
            message.ContentType,
            new Dictionary<string, object?>(message.Headers, StringComparer.OrdinalIgnoreCase)
            {
                ["DeadLetterReason"] = reason,
                ["DeadLetterDescription"] = description,
                ["DeadLetterTime"] = DateTimeOffset.UtcNow.ToString("O")
            },
            new MessageIdentity(message.MessageId, message.CorrelationId, message.ConversationId),
            message.MessageType,
            message.SentTime);

        await transport.SendAsync(deadLetterMessage, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "OutboxDispatcher: Message sent to dead-letter queue. MessageId={MessageId}, Reason={Reason}, Description={Description}",
            message.MessageId,
            reason,
            description);
    }
}
