using Dialysis.BuildingBlocks.Transponder.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Transponder.Transport.AwsSqsSns;

/// <summary>Hosts the SQS long-poll loop and dispatches to <see cref="TransponderConsumeDispatcher"/>.</summary>
public sealed class AwsSqsTransponderConsumerHostedService : BackgroundService
{
    private readonly ITransponderTransport _transport;
    private readonly TransponderConsumeDispatcher _dispatcher;
    private readonly IMessageSerializer _serializer;
    private readonly ITransponderBus _bus;
    private readonly ILogger<AwsSqsTransponderConsumerHostedService> _logger;
    /// <summary>Hosts the SQS long-poll loop and dispatches to <see cref="TransponderConsumeDispatcher"/>.</summary>
    public AwsSqsTransponderConsumerHostedService(ITransponderTransport transport,
        TransponderConsumeDispatcher dispatcher,
        IMessageSerializer serializer,
        ITransponderBus bus,
        ILogger<AwsSqsTransponderConsumerHostedService> logger)
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
            _logger.LogCritical(ex, "Transponder AWS SQS consumer terminated unexpectedly");
            throw;
        }
    }
}
