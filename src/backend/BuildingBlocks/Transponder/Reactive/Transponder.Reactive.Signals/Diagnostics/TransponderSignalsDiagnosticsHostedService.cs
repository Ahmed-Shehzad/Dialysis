using System.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Transponder.Reactive.Signals.Diagnostics;

/// <summary>
/// Owns the lifetimes of the diagnostics bridges: an additive OTel meter
/// (<see cref="MeterName"/>) whose observable gauges pull from the computed signals, and one
/// logging effect that records bus-health transitions. The existing
/// <c>Dialysis.Transponder.Outbox</c> meter and its instruments are untouched (frozen metric
/// contract — audit §5.2); this surface only adds.
/// </summary>
public sealed class TransponderSignalsDiagnosticsHostedService : IHostedService, IDisposable
{
    /// <summary>Meter name to add to <c>ModuleTelemetryOptions.AdditionalMeters</c> in hosts that opt in.</summary>
    public const string MeterName = "Dialysis.Transponder.Signals";

    private readonly TransponderDiagnosticsSignals _signals;
    private readonly ILogger<TransponderSignalsDiagnosticsHostedService> _logger;
    private Meter? _meter;
    private Effect? _healthLogEffect;
    private TransponderBusHealth _lastLoggedHealth = TransponderBusHealth.Unknown;

    /// <summary>Creates the bridge service over the diagnostics signal graph.</summary>
    public TransponderSignalsDiagnosticsHostedService(
        TransponderDiagnosticsSignals signals,
        ILogger<TransponderSignalsDiagnosticsHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(signals);
        ArgumentNullException.ThrowIfNull(logger);
        _signals = signals;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _meter = new Meter(MeterName, "1.0");
        _meter.CreateObservableGauge(
            "dialysis.transponder.bus.health",
            () => (int)_signals.BusHealth.Value,
            unit: "{state}",
            description: "Aggregate Transponder bus health: 0 unknown, 1 healthy, 2 degraded, 3 down.");
        _meter.CreateObservableGauge(
            "dialysis.transponder.outbox.lagging",
            () => _signals.OutboxLagging.Value ? 1 : 0,
            unit: "{boolean}",
            description: "1 when this replica leads the outbox relay and the oldest pending row exceeds the lag threshold.");

        _healthLogEffect = new Effect(
            [_signals.BusHealth],
            () =>
            {
                var health = _signals.BusHealth.Value;
                if (health == _lastLoggedHealth)
                {
                    return;
                }

                _logger.LogInformation(
                    "Transponder bus health transitioned {Previous} -> {Current}.",
                    _lastLoggedHealth,
                    health);
                _lastLoggedHealth = health;
            },
            _logger);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_healthLogEffect is not null)
        {
            await _healthLogEffect.DisposeAsync().ConfigureAwait(false);
            _healthLogEffect = null;
        }

        _meter?.Dispose();
        _meter = null;
    }

    /// <summary>Synchronous safety net when the host skips <see cref="StopAsync"/>.</summary>
    public void Dispose()
    {
        _healthLogEffect?.Dispose();
        _healthLogEffect = null;
        _meter?.Dispose();
        _meter = null;
    }
}
