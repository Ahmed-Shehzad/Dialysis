using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder.Transport.SignalR;

/// <summary>
/// SignalR client to <see cref="TransponderSignalRHub"/>: invokes <see cref="TransponderSignalRHub.PublishMethod"/> and buffers <see cref="TransponderSignalRHub.ReceiveMethod"/> into <see cref="ITransponderTransport.RunConsumerAsync"/>.
/// </summary>
public sealed class SignalRTransponderTransport : ITransponderTransport
{
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private HubConnection? _connection;
    private Channel<TransportMessage>? _inbound;
    private readonly IOptions<TransponderSignalRClientOptions> _options;
    private readonly ILogger<SignalRTransponderTransport> _logger;
    /// <summary>
    /// SignalR client to <see cref="TransponderSignalRHub"/>: invokes <see cref="TransponderSignalRHub.PublishMethod"/> and buffers <see cref="TransponderSignalRHub.ReceiveMethod"/> into <see cref="ITransponderTransport.RunConsumerAsync"/>.
    /// </summary>
    public SignalRTransponderTransport(IOptions<TransponderSignalRClientOptions> options,
        ILogger<SignalRTransponderTransport> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async ValueTask EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_connection?.State == HubConnectionState.Connected)
                return;

            await DisposeCoreAsync().ConfigureAwait(false);

            var o = _options.Value;
            if (string.IsNullOrWhiteSpace(o.HubUrl))
                throw new InvalidOperationException("Transponder SignalR: HubUrl is required (full URL, e.g. https://host/hubs/transponder).");

            var inbound = Channel.CreateUnbounded<TransportMessage>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
            });

            var connection = new HubConnectionBuilder()
                .WithUrl(o.HubUrl.Trim(), ConfigureConnection)
                .WithAutomaticReconnect()
                .Build();

            connection.On<TransponderSignalREnvelopeDto>(TransponderSignalRHub.ReceiveMethod, dto =>
            {
                var msg = ToTransportMessage(dto);
                if (msg is null)
                    return;

                if (!inbound.Writer.TryWrite(msg.Value))
                    _logger.LogWarning("Transponder SignalR: inbound channel closed; dropping message {RoutingKey}", dto.RoutingKey);
            });

            connection.Closed += ex =>
            {
                if (ex is not null)
                    _logger.LogWarning(ex, "Transponder SignalR connection closed with error");
                inbound.Writer.TryComplete(ex);
                return Task.CompletedTask;
            };

            await connection.StartAsync(cancellationToken).ConfigureAwait(false);

            _inbound = inbound;
            _connection = connection;
            _logger.LogInformation("Transponder SignalR client connected to {HubUrl}", o.HubUrl.Trim());
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    public async Task PublishAsync(TransportMessage message, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        var connection = _connection ?? throw new InvalidOperationException("SignalR connection is not initialized.");

        var dto = ToDto(message);
        await connection.InvokeAsync(TransponderSignalRHub.PublishMethod, dto, cancellationToken).ConfigureAwait(false);
    }

    public async Task RunConsumerAsync(
        Func<TransportMessage, CancellationToken, Task> onMessage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(onMessage);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        var inbound = _inbound ?? throw new InvalidOperationException("SignalR inbound channel is not initialized.");

        await foreach (var msg in inbound.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            await onMessage(msg, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await _lifecycle.WaitAsync().ConfigureAwait(false);
        try
        {
            await DisposeCoreAsync().ConfigureAwait(false);
        }
        finally
        {
            _lifecycle.Release();
            _lifecycle.Dispose();
        }
    }

    private void ConfigureConnection(HttpConnectionOptions httpOptions)
    {
        var o = _options.Value;
        if (o.AccessTokenProvider is not null)
            httpOptions.AccessTokenProvider = o.AccessTokenProvider;
    }

    private async Task DisposeCoreAsync()
    {
        _inbound?.Writer.TryComplete();
        _inbound = null;

        if (_connection is not null)
        {
            try
            {
                await _connection.StopAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Transponder SignalR: error stopping connection");
            }

            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }
    }

    private static TransponderSignalREnvelopeDto ToDto(TransportMessage message) => new()
    {
        RoutingKey = message.RoutingKey,
        Payload = message.Payload.ToArray(),
        CorrelationId = message.CorrelationId,
        DeduplicationId = message.DeduplicationId,
        ContentType = message.ContentType,
        Headers = message.Headers is { Count: > 0 }
            ? new Dictionary<string, string>(message.Headers, StringComparer.Ordinal)
            : null,
    };

    private static TransportMessage? ToTransportMessage(TransponderSignalREnvelopeDto dto)
    {
        if (string.IsNullOrEmpty(dto.RoutingKey))
            return null;

        IReadOnlyDictionary<string, string>? headers = dto.Headers is { Count: > 0 }
            ? new Dictionary<string, string>(dto.Headers, StringComparer.Ordinal)
            : null;

        return new TransportMessage(
            dto.RoutingKey,
            dto.Payload,
            dto.CorrelationId,
            dto.ContentType ?? "application/json",
            DeduplicationId: dto.DeduplicationId,
            headers);
    }
}
