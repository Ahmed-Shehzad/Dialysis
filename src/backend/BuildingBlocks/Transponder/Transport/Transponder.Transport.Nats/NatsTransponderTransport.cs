using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Net;

namespace Dialysis.BuildingBlocks.Transponder.Transport.Nats;

/// <summary>
/// NATS <see cref="ITransponderTransport"/> using a single ingress subject and Transponder headers for routing keys.
/// Supports core NATS or JetStream (durable consumer with explicit ack).
/// </summary>
public sealed class NatsTransponderTransport(
    IOptions<TransponderNatsOptions> options,
    ILogger<NatsTransponderTransport> logger) : ITransponderTransport
{
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private NatsClient? _client;
    private INatsJSContext? _jetStream;

    public async ValueTask EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_client is not null)
                return;

            var o = options.Value;
            ValidateJetStreamOptions(o);

            _client = new NatsClient(new NatsOpts { Url = o.Url, Name = o.ClientName });
            await _client.ConnectAsync().ConfigureAwait(false);

            if (o.DeliveryMode == NatsDeliveryMode.JetStream)
            {
                _jetStream = _client.CreateJetStreamContext();
                if (o.JetStreamAutoProvision)
                    await ProvisionJetStreamStreamAsync(o, cancellationToken).ConfigureAwait(false);
            }

            logger.LogInformation(
                "Transponder NATS connected to {Url} ({Mode})",
                o.Url,
                o.DeliveryMode == NatsDeliveryMode.JetStream ? "JetStream" : "core");
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    public async Task PublishAsync(TransportMessage message, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        var client = _client ?? throw new InvalidOperationException("NATS client is not initialized.");
        var o = options.Value;
        var headers = BuildHeaders(message);
        var payload = message.Payload.ToArray();
        var serializer = NatsClientDefaultSerializerRegistry.Default.GetSerializer<byte[]>();

        if (o.DeliveryMode == NatsDeliveryMode.JetStream)
        {
            var js = _jetStream ?? throw new InvalidOperationException("JetStream context is not initialized.");
            var pubOpts = string.IsNullOrEmpty(message.DeduplicationId)
                ? default
                : new NatsJSPubOpts { MsgId = message.DeduplicationId };
            var ack = await js
                .PublishAsync(o.IngressSubject, payload, serializer, pubOpts, headers, cancellationToken)
                .ConfigureAwait(false);
            ack.EnsureSuccess();
        }
        else
        {
            await client
                .PublishAsync(
                    o.IngressSubject,
                    payload,
                    headers,
                    replyTo: null,
                    serializer,
                    opts: default,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public Task RunConsumerAsync(
        Func<TransportMessage, CancellationToken, Task> onMessage,
        CancellationToken cancellationToken) =>
        options.Value.DeliveryMode == NatsDeliveryMode.JetStream
            ? RunJetStreamConsumerAsync(onMessage, cancellationToken)
            : RunCoreConsumerAsync(onMessage, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        await _lifecycle.WaitAsync().ConfigureAwait(false);
        try
        {
            _jetStream = null;
            if (_client is not null)
            {
                await _client.DisposeAsync().ConfigureAwait(false);
                _client = null;
            }
        }
        finally
        {
            _lifecycle.Release();
            _lifecycle.Dispose();
        }
    }

    private static void ValidateJetStreamOptions(TransponderNatsOptions o)
    {
        if (o.DeliveryMode != NatsDeliveryMode.JetStream)
            return;

        if (string.IsNullOrWhiteSpace(o.JetStreamStream))
        {
            throw new InvalidOperationException(
                "Transponder NATS JetStream: JetStreamStream must be set when DeliveryMode is JetStream.");
        }

        if (string.IsNullOrWhiteSpace(o.JetStreamDurable))
        {
            throw new InvalidOperationException(
                "Transponder NATS JetStream: JetStreamDurable must be set when DeliveryMode is JetStream.");
        }
    }

    private async Task ProvisionJetStreamStreamAsync(TransponderNatsOptions o, CancellationToken cancellationToken)
    {
        var js = _jetStream ?? throw new InvalidOperationException("JetStream context is not initialized.");
        var subjects = new List<string> { o.IngressSubject };
        if (o.PoisonMessagePolicy == NatsPoisonMessagePolicy.Republish && !string.IsNullOrWhiteSpace(o.PoisonSubject))
            subjects.Add(o.PoisonSubject.Trim());

        await js
            .CreateOrUpdateStreamAsync(new StreamConfig(o.JetStreamStream!, subjects), cancellationToken)
            .ConfigureAwait(false);
        logger.LogInformation(
            "Transponder NATS JetStream stream {Stream} subjects: {Subjects}",
            o.JetStreamStream,
            string.Join(", ", subjects));
    }

    private async Task RunCoreConsumerAsync(
        Func<TransportMessage, CancellationToken, Task> onMessage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(onMessage);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        var client = _client ?? throw new InvalidOperationException("NATS client is not initialized.");
        var o = options.Value;
        var deserializer = NatsClientDefaultSerializerRegistry.Default.GetDeserializer<byte[]>();

        await foreach (var msg in client
                           .SubscribeAsync<byte[]>(
                               o.IngressSubject,
                               o.QueueGroup,
                               deserializer,
                               cancellationToken: cancellationToken)
                           .ConfigureAwait(false))
        {
            var transportMessage = ToTransportMessage(msg.Headers, msg.Data);
            if (transportMessage is null)
                continue;

            await DispatchCoreAsync(o, client, onMessage, transportMessage.Value, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RunJetStreamConsumerAsync(
        Func<TransportMessage, CancellationToken, Task> onMessage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(onMessage);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        var js = _jetStream ?? throw new InvalidOperationException("JetStream context is not initialized.");
        var o = options.Value;
        var deserializer = NatsClientDefaultSerializerRegistry.Default.GetDeserializer<byte[]>();

        var consumerConfig = new ConsumerConfig(o.JetStreamDurable!)
        {
            FilterSubject = o.IngressSubject,
        };
        if (!string.IsNullOrWhiteSpace(o.QueueGroup))
            consumerConfig.DeliverGroup = o.QueueGroup;

        var consumer = await js
            .CreateOrUpdateConsumerAsync(o.JetStreamStream!, consumerConfig, cancellationToken)
            .ConfigureAwait(false);

        await foreach (var msg in consumer
                           .ConsumeAsync(deserializer, opts: default, cancellationToken: cancellationToken)
                           .ConfigureAwait(false))
        {
            msg.EnsureSuccess();
            var transportMessage = ToTransportMessage(msg.Headers, msg.Data);
            if (transportMessage is null)
            {
                await msg.AckAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                continue;
            }

            try
            {
                await onMessage(transportMessage.Value, cancellationToken).ConfigureAwait(false);
                await msg.AckAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (o.PoisonMessagePolicy == NatsPoisonMessagePolicy.Republish)
                {
                    logger.LogError(ex, "Transponder NATS JetStream handler failed; republishing to poison subject {Poison}", o.PoisonSubject);
                    await PublishPoisonJetStreamAsync(js, o, transportMessage.Value, cancellationToken).ConfigureAwait(false);
                    await msg.AckAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    logger.LogError(ex, "Transponder NATS JetStream handler failed; ack to discard");
                    await msg.AckAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    private async Task DispatchCoreAsync(
        TransponderNatsOptions o,
        NatsClient client,
        Func<TransportMessage, CancellationToken, Task> onMessage,
        TransportMessage transportMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await onMessage(transportMessage, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (o.PoisonMessagePolicy == NatsPoisonMessagePolicy.Republish)
            {
                logger.LogError(ex, "Transponder NATS handler failed; republishing to poison subject {Poison}", o.PoisonSubject);
                await PublishPoisonCoreAsync(client, o, transportMessage, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                logger.LogError(ex, "Transponder NATS handler failed; message dropped (core NATS has no broker ack)");
            }
        }
    }

    private static NatsHeaders BuildHeaders(TransportMessage message)
    {
        var headers = new NatsHeaders { [TransponderTransportHeaderNames.RoutingKey] = message.RoutingKey };
        if (!string.IsNullOrEmpty(message.CorrelationId))
            headers[TransponderTransportHeaderNames.CorrelationId] = message.CorrelationId;
        if (!string.IsNullOrEmpty(message.DeduplicationId))
            headers[TransponderTransportHeaderNames.DeduplicationId] = message.DeduplicationId;
        return headers;
    }

    private static TransportMessage? ToTransportMessage(NatsHeaders? headers, byte[]? body)
    {
        var routingKey = GetHeaderString(headers, TransponderTransportHeaderNames.RoutingKey);
        if (string.IsNullOrEmpty(routingKey))
            return null;

        var correlationId = GetHeaderString(headers, TransponderTransportHeaderNames.CorrelationId);
        var deduplicationId = GetHeaderString(headers, TransponderTransportHeaderNames.DeduplicationId);
        return new TransportMessage(
            routingKey,
            body ?? Array.Empty<byte>(),
            correlationId,
            "application/json",
            DeduplicationId: deduplicationId);
    }

    private async static Task PublishPoisonCoreAsync(
        NatsClient client,
        TransponderNatsOptions o,
        TransportMessage failed,
        CancellationToken cancellationToken)
    {
        var headers = BuildHeaders(failed);
        var serializer = NatsClientDefaultSerializerRegistry.Default.GetSerializer<byte[]>();
        await client
            .PublishAsync(
                o.PoisonSubject,
                failed.Payload.ToArray(),
                headers,
                replyTo: null,
                serializer,
                opts: default,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task PublishPoisonJetStreamAsync(
        INatsJSContext js,
        TransponderNatsOptions o,
        TransportMessage failed,
        CancellationToken cancellationToken)
    {
        var headers = BuildHeaders(failed);
        var serializer = NatsClientDefaultSerializerRegistry.Default.GetSerializer<byte[]>();
        var pubOpts = string.IsNullOrEmpty(failed.DeduplicationId)
            ? default
            : new NatsJSPubOpts { MsgId = failed.DeduplicationId + "-poison" };
        var ack = await js
            .PublishAsync(o.PoisonSubject, failed.Payload.ToArray(), serializer, pubOpts, headers, cancellationToken)
            .ConfigureAwait(false);
        ack.EnsureSuccess();
    }

    private static string? GetHeaderString(NatsHeaders? headers, string name)
    {
        if (headers is null)
            return null;

        var value = headers[name];
        return value.Count == 0 ? null : value.ToString();
    }
}
