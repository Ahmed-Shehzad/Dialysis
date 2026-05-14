using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Transponder.Transport.SignalR;

/// <summary>Hosts the SignalR receive loop and dispatches to <see cref="TransponderConsumeDispatcher"/>.</summary>
public sealed class SignalRTransponderConsumerHostedService(
    ITransponderTransport transport,
    TransponderConsumeDispatcher dispatcher,
    IMessageSerializer serializer,
    ITransponderBus bus,
    ILogger<SignalRTransponderConsumerHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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
            logger.LogCritical(ex, "Transponder SignalR consumer terminated unexpectedly");
            throw;
        }
    }
}
