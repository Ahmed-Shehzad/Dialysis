using Dialysis.BuildingBlocks.Transponder;
using Dialysis.SmartConnect.Contracts.Integration;
using Microsoft.Extensions.Logging;

namespace Dialysis.PDMS.Composition.Demo;

/// <summary>
/// Development-only background service that publishes synthetic
/// <see cref="DialysisMachineTreatmentSnapshotIntegrationEvent"/> and occasional
/// <see cref="DialysisMachineAlarmIntegrationEvent"/> via Transponder. The PDMS
/// <c>TreatmentSnapshotConsumer</c> and <c>TreatmentAlarmConsumer</c> are already wired,
/// so this exercises the full inbound-bridge → consumer path during a demo.
/// </summary>
public sealed class MachineTelemetrySimulatorService : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly Microsoft.Extensions.Logging.ILogger<MachineTelemetrySimulatorService> _logger;
    /// <summary>
    /// Development-only background service that publishes synthetic
    /// <see cref="DialysisMachineTreatmentSnapshotIntegrationEvent"/> and occasional
    /// <see cref="DialysisMachineAlarmIntegrationEvent"/> via Transponder. The PDMS
    /// <c>TreatmentSnapshotConsumer</c> and <c>TreatmentAlarmConsumer</c> are already wired,
    /// so this exercises the full inbound-bridge → consumer path during a demo.
    /// </summary>
    public MachineTelemetrySimulatorService(IServiceProvider services,
        Microsoft.Extensions.Logging.ILogger<MachineTelemetrySimulatorService> logger)
    {
        _services = services;
        _logger = logger;
    }
    private static readonly TimeSpan _interval = TimeSpan.FromSeconds(9);
    private static readonly string[] _machineSerials = ["FX-80-A001", "FX-80-A002", "FX-80-A003"];
    private static readonly long[] _mdcSnapshot = [150020, 150021, 150022, 150023]; // synthetic MDC codes
    private static readonly Random _rng = new(424242);
    private static int _ticks;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Machine telemetry simulator started (every {Seconds}s).", _interval.TotalSeconds);
        try { await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken).ConfigureAwait(false); }
        catch (TaskCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Machine telemetry simulator tick failed.");
            }
            try { await Task.Delay(_interval, stoppingToken).ConfigureAwait(false); }
            catch (TaskCanceledException) { return; }
        }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        using var scope = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(_services);
        var bus = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
            .GetRequiredService<ITransponderBus>(scope.ServiceProvider);

        var seq = Interlocked.Increment(ref _ticks);
        var machine = _machineSerials[seq % _machineSerials.Length];
        var now = DateTime.UtcNow;

        var observations = _mdcSnapshot.Select(mdc => new NormalizedMachineObservation(
            MdcCode: mdc,
            ContainmentPath: $"1.1.4.{mdc % 100}.1",
            NumericValue: (decimal)(_rng.NextDouble() * 200d),
            StringValue: null,
            Units: "mmHg",
            ProfileValues: null,
            ProfileTimesSeconds: null,
            ObservedAtUtc: now)).ToArray();

        var snapshot = new DialysisMachineTreatmentSnapshotIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: now,
            SchemaVersion: 1,
            MachineSerial: machine,
            VendorCode: "FRES",
            ModelCode: "FX-80",
            SourceMessageId: Guid.CreateVersion7(),
            MessageControlId: $"SIM-SNAP-{seq:D8}",
            ObservedAtUtc: now,
            PatientMrn: $"MRN-{1000 + seq % 5:D4}",
            FillerOrderNumber: null,
            Observations: observations);
        await bus.PublishAsync(snapshot, cancellationToken).ConfigureAwait(false);

        // Roughly every 6th tick, also emit an alarm to exercise the alarm consumer.
        if (seq % 6 == 0)
        {
            var alarm = new DialysisMachineAlarmIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                OccurredOn: now,
                SchemaVersion: 1,
                MachineSerial: machine,
                SourceMessageId: Guid.CreateVersion7(),
                MessageControlId: $"SIM-ALRM-{seq:D8}",
                ObservedAtUtc: now,
                AlarmCode: 196610L,
                AlarmSource: "ARTERIAL_PRESSURE_LOW",
                AlarmPhase: "TREATMENT",
                State: DialysisMachineAlarmState.Present,
                AlarmObservations: []);
            await bus.PublishAsync(alarm, cancellationToken).ConfigureAwait(false);
        }
    }
}
