using Google.Protobuf;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder.Transport.Grpc;

/// <summary>
/// gRPC client to a <see cref="TransponderGrpcIngressService"/> relay: unary publish and server-streaming subscribe.
/// </summary>
public sealed class GrpcTransponderTransport(
    IOptions<TransponderGrpcClientOptions> options,
    ILogger<GrpcTransponderTransport> logger) : ITransponderTransport
{
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private GrpcChannel? _channel;
    private TransponderIngress.TransponderIngressClient? _client;

    public async ValueTask EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_channel is not null && _client is not null)
                return;

            await DisposeCoreAsync().ConfigureAwait(false);

            var o = options.Value;
            if (string.IsNullOrWhiteSpace(o.Address))
                throw new InvalidOperationException("Transponder gRPC: Address is required (e.g. https://localhost:7123).");

            var address = o.Address.Trim();
            var httpHandler = new SocketsHttpHandler { EnableMultipleHttp2Connections = true };
            if (o.ForDevelopmentOnlyDisableCertificateValidation)
            {
                httpHandler.SslOptions.RemoteCertificateValidationCallback = static (_, _, _, _) => true;
            }

            _channel = GrpcChannel.ForAddress(
                address,
                new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = o.MaxReceiveMessageSizeBytes,
                    MaxSendMessageSize = o.MaxSendMessageSizeBytes,
                });
            _client = new TransponderIngress.TransponderIngressClient(_channel);
            logger.LogInformation("Transponder gRPC client connected to {Address}", address);
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    public async Task PublishAsync(TransportMessage message, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        var client = _client ?? throw new InvalidOperationException("gRPC client is not initialized.");

        var envelope = ToEnvelope(message);
        await client.PublishAsync(envelope, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task RunConsumerAsync(
        Func<TransportMessage, CancellationToken, Task> onMessage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(onMessage);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        var client = _client ?? throw new InvalidOperationException("gRPC client is not initialized.");
        var o = options.Value;

        using var streamingCall = client.Subscribe(
            new SubscribeRequest { ClientName = o.ClientName ?? string.Empty },
            cancellationToken: cancellationToken);

        logger.LogInformation("Transponder gRPC subscribe stream open");

        var stream = streamingCall.ResponseStream;
        try
        {
            while (await stream.MoveNext(cancellationToken).ConfigureAwait(false))
            {
                var env = stream.Current;
                var transport = ToTransportMessage(env);
                if (transport is null)
                {
                    logger.LogWarning("Transponder gRPC: envelope missing routing_key; skipping");
                    continue;
                }

                await onMessage(transport.Value, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // shutdown
        }
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

    private async Task DisposeCoreAsync()
    {
        if (_channel is not null)
        {
            _channel.Dispose();
            _channel = null;
            _client = null;
        }
    }

    private static TransportEnvelope ToEnvelope(TransportMessage message)
    {
        var env = new TransportEnvelope
        {
            RoutingKey = message.RoutingKey,
            Payload = ByteString.CopyFrom(message.Payload.Span),
            CorrelationId = message.CorrelationId ?? string.Empty,
            DeduplicationId = message.DeduplicationId ?? string.Empty,
            ContentType = message.ContentType ?? "application/json",
        };

        if (message.Headers is { Count: > 0 })
        {
            foreach (var kv in message.Headers)
                env.Headers[kv.Key] = kv.Value;
        }

        return env;
    }

    private static TransportMessage? ToTransportMessage(TransportEnvelope env)
    {
        if (string.IsNullOrEmpty(env.RoutingKey))
            return null;

        IReadOnlyDictionary<string, string>? headers = env.Headers.Count > 0
            ? new Dictionary<string, string>(env.Headers, StringComparer.Ordinal)
            : null;

        return new TransportMessage(
            env.RoutingKey,
            env.Payload.Memory,
            string.IsNullOrEmpty(env.CorrelationId) ? null : env.CorrelationId,
            string.IsNullOrEmpty(env.ContentType) ? "application/json" : env.ContentType,
            DeduplicationId: string.IsNullOrEmpty(env.DeduplicationId) ? null : env.DeduplicationId,
            headers);
    }
}
