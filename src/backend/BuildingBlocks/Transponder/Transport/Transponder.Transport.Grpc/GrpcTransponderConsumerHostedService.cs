using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Transponder.Transport.Grpc;

/// <summary>Hosts the gRPC subscribe stream and dispatches to <see cref="TransponderConsumeDispatcher"/>.</summary>
public sealed class GrpcTransponderConsumerHostedService(
    ITransponderTransport transport,
    TransponderConsumeDispatcher dispatcher,
    IMessageSerializer serializer,
    ITransponderBus bus,
    ILogger<GrpcTransponderConsumerHostedService> logger) : BackgroundService
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
            logger.LogCritical(ex, "Transponder gRPC consumer terminated unexpectedly");
            throw;
        }
    }
}
