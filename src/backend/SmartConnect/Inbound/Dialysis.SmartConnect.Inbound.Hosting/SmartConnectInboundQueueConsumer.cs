using Dialysis.SmartConnect.Inbound;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.SmartConnect.Inbound.Hosting;

/// <summary>
/// Background worker that reads <see cref="InboundQueueItem"/> from <see cref="IInboundQueueSubscription"/>,
/// builds <see cref="IntegrationMessage"/> via <see cref="IInboundMessageFactory"/>, and dispatches via <see cref="IInboundTransport"/>.
/// </summary>
public sealed class SmartConnectInboundQueueConsumer(
    IInboundQueueSubscription queue,
    IInboundMessageFactory messageFactory,
    IInboundTransport inboundTransport,
    ILogger<SmartConnectInboundQueueConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            InboundQueueItem? item;
            try
            {
                item = await queue.ReadAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (item is null)
                break;

            try
            {
                var msg = messageFactory.Create(
                    item.FlowId,
                    item.Payload,
                    item.PayloadFormat,
                    item.CorrelationId,
                    item.Metadata.IsEmpty ? null : item.Metadata);

                await inboundTransport.DispatchAsync(msg, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SmartConnect inbound queue dispatch failed for flow {FlowId}.", item.FlowId);
            }
        }
    }
}
