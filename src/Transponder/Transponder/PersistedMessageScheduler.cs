using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using Transponder.Abstractions;
using Transponder.Persistence;
using Transponder.Persistence.Abstractions;
using Transponder.Transports;
using Transponder.Transports.Abstractions;

namespace Transponder;

/// <summary>
/// Persists scheduled messages and dispatches them when due.
/// </summary>
public sealed class PersistedMessageScheduler : IMessageScheduler, IAsyncDisposable
{
    private readonly TransponderBus _bus;
    private readonly IScheduledMessageStore _store;
    private readonly IMessageSerializer _serializer;
    private readonly ILogger<PersistedMessageScheduler> _logger;
    private readonly ITransportHostProvider _hostProvider;
    private readonly PersistedMessageSchedulerOptions _options;
    private readonly Uri? _deadLetterAddress;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loop;
    private int _disposed;

    public PersistedMessageScheduler(
        TransponderBus bus,
        IScheduledMessageStore store,
        IMessageSerializer serializer,
        ITransportHostProvider hostProvider,
        PersistedMessageSchedulerOptions? options = null,
        ILogger<PersistedMessageScheduler>? logger = null)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _hostProvider = hostProvider ?? throw new ArgumentNullException(nameof(hostProvider));
        _options = options ?? new PersistedMessageSchedulerOptions();
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PersistedMessageScheduler>.Instance;
        _deadLetterAddress = _options.DeadLetterAddress;

        _loop = Task.Run(() => DispatchLoopAsync(_cts.Token), _cts.Token);
    }

    public Task<IScheduledMessageHandle> ScheduleSendAsync<TMessage>(
        Uri destinationAddress,
        TMessage message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken = default)
        where TMessage : class, IMessage
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(message);

        return scheduledTime <= DateTimeOffset.UtcNow
            ? throw new ArgumentException("Scheduled time must be in the future.", nameof(scheduledTime))
            : ScheduleSendInternalAsync(destinationAddress, message, scheduledTime, cancellationToken);
    }

    public Task<IScheduledMessageHandle> ScheduleSendAsync<TMessage>(
        Uri destinationAddress,
        TMessage message,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
        where TMessage : class, IMessage
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(message);

        if (delay <= TimeSpan.Zero) throw new ArgumentException("Delay must be greater than zero.", nameof(delay));

        var scheduledTime = DateTimeOffset.UtcNow.Add(delay);
        return ScheduleSendInternalAsync(destinationAddress, message, scheduledTime, cancellationToken);
    }

    public Task<IScheduledMessageHandle> SchedulePublishAsync<TMessage>(
        TMessage message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken = default)
        where TMessage : class, IMessage
    {
        ArgumentNullException.ThrowIfNull(message);

        return scheduledTime <= DateTimeOffset.UtcNow ? throw new ArgumentException("Scheduled time must be in the future.", nameof(scheduledTime)) : SchedulePublishInternalAsync(message, scheduledTime, cancellationToken);
    }

    public Task<IScheduledMessageHandle> SchedulePublishAsync<TMessage>(
        TMessage message,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
        where TMessage : class, IMessage
    {
        ArgumentNullException.ThrowIfNull(message);

        if (delay <= TimeSpan.Zero) throw new ArgumentException("Delay must be greater than zero.", nameof(delay));

        var scheduledTime = DateTimeOffset.UtcNow.Add(delay);
        return SchedulePublishInternalAsync(message, scheduledTime, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try
        {
            await _cts.CancelAsync().ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        try
        {
            await _loop.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }

        _cts.Dispose();
    }

    private Task<IScheduledMessageHandle> SchedulePublishInternalAsync<TMessage>(
        TMessage message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken)
        where TMessage : class, IMessage
        => ScheduleInternalAsync(message, scheduledTime, null, cancellationToken);

    private Task<IScheduledMessageHandle> ScheduleSendInternalAsync<TMessage>(
        Uri destinationAddress,
        TMessage message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken)
        where TMessage : class, IMessage
    {
        var headers = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            [TransponderMessageHeaders.DestinationAddress] = destinationAddress.ToString()
        };

        return ScheduleInternalAsync(message, scheduledTime, headers, cancellationToken);
    }

    private async Task<IScheduledMessageHandle> ScheduleInternalAsync<TMessage>(
        TMessage message,
        DateTimeOffset scheduledTime,
        IReadOnlyDictionary<string, object?>? headers,
        CancellationToken cancellationToken)
        where TMessage : class, IMessage
    {
        var messageType = message.GetType();
        var body = await _serializer.SerializeAsync(message, messageType, cancellationToken);
        var tokenId = Ulid.NewUlid();

        var stored = new ScheduledMessage(
            tokenId,
            messageType.AssemblyQualifiedName ?? messageType.FullName ?? messageType.Name,
            body,
            scheduledTime,
            headers,
            contentType: _serializer.ContentType);

        await _store.AddAsync(stored, cancellationToken).ConfigureAwait(false);

        IScheduledMessageHandle handle = new PersistedScheduledMessageHandle(_store, tokenId);
        return handle;
    }

    private async Task DispatchLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await DispatchDueMessagesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await Task.Delay(_options.PollInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DispatchDueMessagesAsync(CancellationToken cancellationToken)
    {
        var due = await _store.GetDueAsync(DateTimeOffset.UtcNow, _options.BatchSize, cancellationToken)
            .ConfigureAwait(false);

        foreach (var message in due)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessDueMessageAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessDueMessageAsync(IScheduledMessage message, CancellationToken cancellationToken)
    {
        if (await TryHandleInvalidMessageAsync(message, ValidateMessageType(message), cancellationToken).ConfigureAwait(false))
            return;

        var messageType = ResolveMessageType(message.MessageType);
        if (messageType is null && await TryHandleInvalidMessageAsync(message, $"Message type '{message.MessageType}' could not be resolved.", cancellationToken).ConfigureAwait(false))
            return;

        (var payload, var deserializeError) = TryDeserializePayload(message, messageType!);
        if (await TryHandleInvalidMessageAsync(message, deserializeError, cancellationToken).ConfigureAwait(false))
            return;

        if (!TryResolveDestinationAddress(message, out var destinationAddress, out var invalidReason) && await TryHandleInvalidMessageAsync(message, invalidReason!, cancellationToken).ConfigureAwait(false))
            return;

        await DispatchMessageAsync(message, payload!, destinationAddress, cancellationToken).ConfigureAwait(false);
    }

    private static string? ValidateMessageType(IScheduledMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.MessageType))
            return null;

        return "Message type is null or empty.";
    }

    private (object? payload, string? error) TryDeserializePayload(IScheduledMessage message, Type messageType)
    {
        try
        {
            var payload = _serializer.Deserialize(message.Body.Span, messageType);
            return (payload, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "PersistedMessageScheduler: Failed to deserialize message. TokenId={TokenId}, MessageType={MessageType}",
                message.TokenId,
                message.MessageType ?? "unknown");
            return (null, $"Failed to deserialize message: {ex.Message}");
        }
    }

    private bool TryResolveDestinationAddress(IScheduledMessage message, out Uri? destinationAddress, out string? invalidReason)
    {
        destinationAddress = null;
        invalidReason = null;

        if (!message.Headers.TryGetValue(TransponderMessageHeaders.DestinationAddress, out var destinationValue))
            return true;

        if (TryParseDestinationAddress(destinationValue, out var parsed))
        {
            destinationAddress = parsed;
            return true;
        }

        _logger.LogWarning(
            "PersistedMessageScheduler: Failed to parse destination address. TokenId={TokenId}, DestinationValue={DestinationValue}",
            message.TokenId,
            destinationValue);
        invalidReason = $"Failed to parse destination address: {destinationValue}";
        return false;
    }

    private async Task<bool> TryHandleInvalidMessageAsync(
        IScheduledMessage message,
        string? reason,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return false;

        _logger.LogWarning(
            "PersistedMessageScheduler: Invalid message. TokenId={TokenId}, Reason={Reason}",
            message.TokenId,
            reason);

        if (_deadLetterAddress is null)
            return true;

        await SendToDeadLetterQueueAsync(message, "InvalidMessage", reason, cancellationToken).ConfigureAwait(false);
        await _store.MarkDispatchedAsync(message.TokenId, DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private async Task DispatchMessageAsync(
        IScheduledMessage message,
        object payload,
        Uri? destinationAddress,
        CancellationToken cancellationToken)
    {
        try
        {
            if (destinationAddress is null)
                await _bus.PublishObjectAsync(payload, message.Headers, cancellationToken).ConfigureAwait(false);
            else
            {
                var dispatchHeaders = RemoveDestinationHeader(message.Headers);
                await _bus.SendObjectAsync(destinationAddress, payload, dispatchHeaders, cancellationToken).ConfigureAwait(false);
            }

            await _store.MarkDispatchedAsync(message.TokenId, DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "PersistedMessageScheduler: Failed to dispatch message, will retry. TokenId={TokenId}, MessageType={MessageType}, DestinationAddress={DestinationAddress}",
                message.TokenId,
                message.MessageType ?? "unknown",
                destinationAddress?.ToString() ?? "publish");
        }
    }

    private static Type? ResolveMessageType(string messageType) => string.IsNullOrWhiteSpace(messageType) ? null : Type.GetType(messageType, throwOnError: false);

    private static bool TryParseDestinationAddress(object? value, out Uri? destinationAddress)
    {
        destinationAddress = null;

        var address = value switch
        {
            null => null,
            Uri uri => uri.ToString(),
            string text => text,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            JsonElement => null,
            _ => value.ToString()
        };

        return !string.IsNullOrWhiteSpace(address) && Uri.TryCreate(address, UriKind.RelativeOrAbsolute, out destinationAddress);
    }

    private static IReadOnlyDictionary<string, object?> RemoveDestinationHeader(
        IReadOnlyDictionary<string, object?> headers)
    {
        var filtered = new Dictionary<string, object?>(headers, StringComparer.OrdinalIgnoreCase);
        _ = filtered.Remove(TransponderMessageHeaders.DestinationAddress);
        return filtered;
    }

    private sealed class PersistedScheduledMessageHandle : IScheduledMessageHandle
    {
        private readonly IScheduledMessageStore _store;

        public PersistedScheduledMessageHandle(IScheduledMessageStore store, Ulid tokenId)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            if (tokenId == Ulid.Empty) throw new ArgumentException("TokenId must be provided.", nameof(tokenId));

            TokenId = tokenId;
        }

        public Ulid TokenId { get; }

        public Task CancelAsync(CancellationToken cancellationToken = default)
            => _store.CancelAsync(TokenId, cancellationToken);
    }

    private async Task SendToDeadLetterQueueAsync(
        IScheduledMessage message,
        string reason,
        string description,
        CancellationToken cancellationToken)
    {
        if (_deadLetterAddress is null) return;

        var host = _hostProvider.GetHost(_deadLetterAddress);
        var transport = await host.GetSendTransportAsync(_deadLetterAddress, cancellationToken)
            .ConfigureAwait(false);

        var deadLetterMessage = new TransportMessage(
            message.Body,
            message.ContentType ?? "application/json",
            new Dictionary<string, object?>(message.Headers, StringComparer.OrdinalIgnoreCase)
            {
                ["DeadLetterReason"] = reason,
                ["DeadLetterDescription"] = description,
                ["DeadLetterTime"] = DateTimeOffset.UtcNow.ToString("O"),
                ["ScheduledTokenId"] = message.TokenId.ToString()
            },
            Ulid.NewUlid(),
            null,
            null,
            message.MessageType,
            DateTimeOffset.UtcNow);

        await transport.SendAsync(deadLetterMessage, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "PersistedMessageScheduler: Message sent to dead-letter queue. TokenId={TokenId}, Reason={Reason}, Description={Description}",
            message.TokenId,
            reason,
            description);
    }
}
