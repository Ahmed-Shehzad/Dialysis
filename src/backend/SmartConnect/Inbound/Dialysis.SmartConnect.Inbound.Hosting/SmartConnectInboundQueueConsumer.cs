using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.SmartConnect.Inbound.Hosting;

/// <summary>
/// Background worker that reads <see cref="InboundQueueItem"/> from <see cref="IInboundQueueSubscription"/>,
/// builds <see cref="IntegrationMessage"/> via <see cref="IInboundMessageFactory"/>, and dispatches via <see cref="IInboundTransport"/>.
/// </summary>
public sealed class SmartConnectInboundQueueConsumer : BackgroundService
{
    private readonly IInboundQueueSubscription _queue;
    private readonly IInboundMessageFactory _messageFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SmartConnectInboundQueueConsumer> _logger;
    /// <summary>
    /// Background worker that reads <see cref="InboundQueueItem"/> from <see cref="IInboundQueueSubscription"/>,
    /// builds <see cref="IntegrationMessage"/> via <see cref="IInboundMessageFactory"/>, and dispatches via <see cref="IInboundTransport"/>.
    /// </summary>
    public SmartConnectInboundQueueConsumer(IInboundQueueSubscription queue,
        IInboundMessageFactory messageFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<SmartConnectInboundQueueConsumer> logger)
    {
        _queue = queue;
        _messageFactory = messageFactory;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            InboundQueueItem? item;
            try
            {
                item = await _queue.ReadAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (item is null)
                break;

            try
            {
                var msg = _messageFactory.Create(
                    item.FlowId,
                    item.Payload,
                    item.PayloadFormat,
                    item.CorrelationId,
                    item.Metadata.IsEmpty ? null : item.Metadata);

                await using var scope = _scopeFactory.CreateAsyncScope();
                var inboundTransport = scope.ServiceProvider.GetRequiredService<IInboundTransport>();
                await inboundTransport.DispatchAsync(msg, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SmartConnect inbound queue dispatch failed for flow {FlowId}.", item.FlowId);
            }
        }
    }
}
