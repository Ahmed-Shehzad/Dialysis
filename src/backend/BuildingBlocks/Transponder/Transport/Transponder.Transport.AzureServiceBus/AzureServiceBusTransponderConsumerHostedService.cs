using Dialysis.BuildingBlocks.Transponder.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Transponder.Transport.AzureServiceBus;

/// <summary>Hosts the Service Bus subscription processor and dispatches to <see cref="TransponderConsumeDispatcher"/>.</summary>
public sealed class AzureServiceBusTransponderConsumerHostedService : BackgroundService
{
    private readonly ITransponderTransport _transport;
    private readonly TransponderConsumeDispatcher _dispatcher;
    private readonly IMessageSerializer _serializer;
    private readonly ITransponderBus _bus;
    private readonly ILogger<AzureServiceBusTransponderConsumerHostedService> _logger;
    /// <summary>Hosts the Service Bus subscription processor and dispatches to <see cref="TransponderConsumeDispatcher"/>.</summary>
    public AzureServiceBusTransponderConsumerHostedService(ITransponderTransport transport,
        TransponderConsumeDispatcher dispatcher,
        IMessageSerializer serializer,
        ITransponderBus bus,
        ILogger<AzureServiceBusTransponderConsumerHostedService> logger)
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
            _logger.LogCritical(ex, "Transponder Azure Service Bus consumer terminated unexpectedly");
            throw;
        }
    }
}
