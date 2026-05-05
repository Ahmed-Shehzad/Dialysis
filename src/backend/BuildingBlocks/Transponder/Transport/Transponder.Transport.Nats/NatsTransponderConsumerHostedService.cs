using Dialysis.BuildingBlocks.Transponder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Transponder.Transport.Nats;

/// <summary>
/// Hosts the NATS subscription loop and dispatches to registered <see cref="IConsumer{T}"/> handlers.
/// </summary>
public sealed class NatsTransponderConsumerHostedService(
    ITransponderTransport transport,
    TransponderConsumeDispatcher dispatcher,
    IMessageSerializer serializer,
    ITransponderBus bus,
    ILogger<NatsTransponderConsumerHostedService> logger) : BackgroundService
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
            logger.LogCritical(ex, "Transponder NATS consumer terminated unexpectedly");
            throw;
        }
    }
}
