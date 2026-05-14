using System.Collections.Immutable;
using Dialysis.BuildingBlocks.Transponder.Transport;
using Dialysis.SmartConnect.Inbound.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.SmartConnect.Inbound.Transponder;

/// <summary>
/// Runs <see cref="ITransponderTransport.RunConsumerAsync"/> and enqueues each payload to <see cref="ChannelInboundQueue"/>.
/// Host must register <see cref="ITransponderTransport"/>, <see cref="ChannelInboundQueue"/>, and <see cref="SmartConnectInboundQueueConsumer"/>.
/// </summary>
public sealed class TransponderInboundTransportBridge(
    ITransponderTransport transport,
    ChannelInboundQueue channelInboundQueue,
    IOptionsMonitor<TransponderInboundBridgeOptions> options,
    ILogger<TransponderInboundTransportBridge> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opt = options.CurrentValue;
        if (opt.DefaultFlowId == Guid.Empty)
        {
            logger.LogWarning("Transponder inbound bridge disabled: DefaultFlowId is empty.");
            return;
        }

        await transport.EnsureConnectedAsync(stoppingToken).ConfigureAwait(false);
        logger.LogInformation(
            "SmartConnect Transponder inbound bridge consuming for flow {FlowId}.",
            opt.DefaultFlowId);

        await transport.RunConsumerAsync(
            async (msg, ct) =>
            {
                var format = ResolveFormat(msg, opt);
                var metadata = ImmutableDictionary<string, string>.Empty;
                if (msg.Headers is not null)
                {
                    foreach (var kv in msg.Headers)
                    {
                        metadata = metadata.Add(kv.Key, kv.Value);
                    }
                }
                await channelInboundQueue.Writer.WriteAsync(
                    new InboundQueueItem
                    {
                        FlowId = opt.DefaultFlowId,
                        Payload = msg.Payload.ToArray(),
                        PayloadFormat = format,
                        CorrelationId = msg.CorrelationId,
                        Metadata = metadata,
                    },
                    ct).ConfigureAwait(false);
            },
            stoppingToken).ConfigureAwait(false);
    }

    private static PayloadFormat ResolveFormat(TransportMessage msg, TransponderInboundBridgeOptions opt)
    {
        if (opt.TreatJsonContentTypeAsJson &&
            msg.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
        {
            return PayloadFormat.Json;
        }

        return PayloadFormat.Binary;
    }
}
