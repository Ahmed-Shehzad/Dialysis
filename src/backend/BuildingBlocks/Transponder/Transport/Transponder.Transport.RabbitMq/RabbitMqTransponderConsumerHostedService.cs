using Dialysis.BuildingBlocks.Transponder.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;

/// <summary>
/// Hosts the RabbitMQ consumer loop and dispatches to registered <see cref="IConsumer{T}"/> handlers.
/// </summary>
public sealed class RabbitMqTransponderConsumerHostedService : BackgroundService
{
    private readonly ITransponderTransport _transport;
    private readonly TransponderConsumeDispatcher _dispatcher;
    private readonly IMessageSerializer _serializer;
    private readonly ITransponderBus _bus;
    private readonly ILogger<RabbitMqTransponderConsumerHostedService> _logger;
    /// <summary>
    /// Hosts the RabbitMQ consumer loop and dispatches to registered <see cref="IConsumer{T}"/> handlers.
    /// </summary>
    public RabbitMqTransponderConsumerHostedService(ITransponderTransport transport,
        TransponderConsumeDispatcher dispatcher,
        IMessageSerializer serializer,
        ITransponderBus bus,
        ILogger<RabbitMqTransponderConsumerHostedService> logger)
    {
        _transport = transport;
        _dispatcher = dispatcher;
        _serializer = serializer;
        _bus = bus;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _transport
                .RunConsumerAsync(
                    (msg, ct) => _dispatcher.DispatchAsync(
                        msg.RoutingKey,
                        msg.Payload,
                        msg.CorrelationId,
                        msg.DeduplicationId,
                        _serializer,
                        _bus,
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
            _logger.LogCritical(ex, "Transponder RabbitMQ consumer terminated unexpectedly");
            throw;
        }
    }
}
