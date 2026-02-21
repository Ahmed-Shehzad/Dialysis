using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Transponder;
using Transponder.Transports.Abstractions;

namespace Dialysis.Alarm.Infrastructure.IntegrationEvents;

/// <summary>
/// ASB receive endpoint for ThresholdBreachDetectedIntegrationEvent. Applies Inbox pattern for idempotent consumption.
/// </summary>
internal sealed class ThresholdBreachDetectedReceiveEndpoint : IReceiveEndpoint
{
    private const string TopicName = "ThresholdBreachDetectedIntegrationEvent";
    private const string SubscriptionName = "alarm-threshold-breach";

    private readonly Uri _alarmBusAddress;
    private readonly ITransportHostProvider _hostProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ThresholdBreachDetectedReceiveEndpoint> _logger;
    private IReceiveEndpoint? _innerEndpoint;

    public ThresholdBreachDetectedReceiveEndpoint(
        Uri alarmBusAddress,
        ITransportHostProvider hostProvider,
        IServiceScopeFactory scopeFactory,
        ILogger<ThresholdBreachDetectedReceiveEndpoint> logger)
    {
        _alarmBusAddress = alarmBusAddress ?? throw new ArgumentNullException(nameof(alarmBusAddress));
        _hostProvider = hostProvider ?? throw new ArgumentNullException(nameof(hostProvider));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        InputAddress = new Uri(_alarmBusAddress, $"{TopicName}/subscriptions/{SubscriptionName}");
    }

    public Uri InputAddress { get; }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        ITransportHost host = _hostProvider.GetHost(_alarmBusAddress);
        var configuration = new ReceiveEndpointConfiguration(
            InputAddress,
            async ctx =>
            {
                await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
                ThresholdBreachDetectedReceiveHandler handler = scope.ServiceProvider.GetRequiredService<ThresholdBreachDetectedReceiveHandler>();
                await handler.HandleAsync(ctx, ctx.CancellationToken).ConfigureAwait(false);
            });

        _innerEndpoint = host.ConnectReceiveEndpoint(configuration);
        _logger.LogInformation(
            "ThresholdBreachDetectedReceiveEndpoint started. InputAddress={InputAddress}",
            InputAddress);
        return _innerEndpoint.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
        => _innerEndpoint?.StopAsync(cancellationToken) ?? Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        if (_innerEndpoint is not null)
        {
            await _innerEndpoint.DisposeAsync().ConfigureAwait(false);
            _innerEndpoint = null;
        }
    }
}
