using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;

/// <summary>
/// Hosts the RabbitMQ consumer loop and dispatches to registered <see cref="IConsumer{T}"/> handlers.
/// </summary>
public sealed class RabbitMqTransponderConsumerHostedService(
    ITransponderTransport transport,
    TransponderConsumeDispatcher dispatcher,
    IMessageSerializer serializer,
    ITransponderBus bus,
    ILogger<RabbitMqTransponderConsumerHostedService> logger) : BackgroundService
{
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await transport
                .RunConsumerAsync(
                    (msg, ct) => dispatcher.DispatchAsync(
                        msg.RoutingKey,
                        msg.Payload,
                        msg.CorrelationId,
                        msg.DeduplicationId,
                        serializer,
                        bus,
                        ct),
                    stoppingToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // normal shutdown
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Transponder RabbitMQ consumer terminated unexpectedly");
            throw;
        }
    }
}
