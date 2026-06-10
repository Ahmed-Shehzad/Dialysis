using Dialysis.SmartConnect.Alerts;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect;

/// <summary>
/// Publishes <see cref="AlertTrigger"/>s for the flow runtime fire-and-forget so the alert
/// engine never blocks the dispatch path.
/// </summary>
internal sealed class FlowAlertPublisher
{
    private readonly IAlertSink? _alertSink;
    private readonly TimeProvider _time;
    private readonly IServiceScopeFactory? _scopeFactory;

    /// <summary>
    /// Publishes <see cref="AlertTrigger"/>s for the flow runtime fire-and-forget so the alert
    /// engine never blocks the dispatch path.
    /// </summary>
    public FlowAlertPublisher(IAlertSink? alertSink, TimeProvider time, IServiceScopeFactory? scopeFactory)
    {
        _alertSink = alertSink;
        _time = time;
        _scopeFactory = scopeFactory;
    }

    /// <summary>Publishes one alert trigger for <paramref name="message"/> without awaiting the sink.</summary>
    public void Publish(IntegrationMessage message, AlertErrorType errorType, string? errorDetail, CancellationToken cancellationToken)
    {
        if (_alertSink is null)
            return;
        var trigger = new AlertTrigger
        {
            FlowId = message.FlowId,
            MessageId = message.Id,
            CorrelationId = message.CorrelationId,
            ErrorType = errorType,
            ErrorDetail = errorDetail,
            OccurredAtUtc = _time.GetUtcNow(),
        };
        // Fire-and-forget: alerts must never block the dispatch path. When a scope factory is
        // available, the background task runs the alert sink in a fresh DI scope so its EF
        // bookkeeping (alert-event store, rule repository) gets its own DbContext — sharing the
        // dispatcher's scoped DbContext would race with the in-flight ledger save and surface as
        // "Collection was modified during enumeration" inside ChangeTracker.DetectChanges.
        //
        // Fallback to the captured singleton-style alertSink when no scope factory is wired (legacy
        // tests that compose the runtime by hand without a DI container). The legacy path remains
        // safe only when the consumer doesn't share a DbContext between threads.
        _ = Task.Run(async () =>
        {
            try
            {
                if (_scopeFactory is not null)
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var scopedSink = scope.ServiceProvider.GetService<IAlertSink>() ?? _alertSink;
                    await scopedSink.PublishAsync(trigger, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await _alertSink.PublishAsync(trigger, cancellationToken).ConfigureAwait(false);
                }
            }
            catch { /* swallowed: alert engine logs internally */ }
        }, cancellationToken);
    }
}
