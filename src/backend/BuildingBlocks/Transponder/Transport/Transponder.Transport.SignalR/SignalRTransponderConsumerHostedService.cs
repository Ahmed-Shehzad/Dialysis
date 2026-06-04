using Dialysis.BuildingBlocks.Transponder.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Transponder.Transport.SignalR;

/// <summary>Hosts the SignalR receive loop and dispatches to <see cref="TransponderConsumeDispatcher"/>.</summary>
public sealed class SignalRTransponderConsumerHostedService : BackgroundService
{
    private readonly ITransponderTransport _transport;
    private readonly TransponderConsumeDispatcher _dispatcher;
    private readonly IMessageSerializer _serializer;
    private readonly ITransponderBus _bus;
    private readonly ILogger<SignalRTransponderConsumerHostedService> _logger;
    /// <summary>Hosts the SignalR receive loop and dispatches to <see cref="TransponderConsumeDispatcher"/>.</summary>
    public SignalRTransponderConsumerHostedService(ITransponderTransport transport,
        TransponderConsumeDispatcher dispatcher,
        IMessageSerializer serializer,
        ITransponderBus bus,
        ILogger<SignalRTransponderConsumerHostedService> logger)
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
            _logger.LogCritical(ex, "Transponder SignalR consumer terminated unexpectedly");
            throw;
        }
    }
}
