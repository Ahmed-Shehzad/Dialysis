using Dialysis.Module.Contracts.Billing;
using Dialysis.PDMS.TreatmentSessions.Domain;
using Dialysis.PDMS.TreatmentSessions.Ports;
using Dialysis.PDMS.TreatmentSessions.Realtime;

namespace Dialysis.PDMS.Api.Realtime;

/// <summary>
/// Pushes a running, itemised cost estimate to each in-progress session's chairside over the
/// vitals SignalR hub (message <c>"cost"</c>). It ticks on a fixed cadence rather than per
/// reading so the chairside number advances smoothly regardless of reading frequency.
///
/// The estimate prorates ultrafiltration against the prescription; the authoritative charge is
/// captured by EHR.Billing when the session completes. Both sides use the same
/// <see cref="DialysisTariff"/>, so the live total converges to the invoice total.
/// </summary>
public sealed class SessionCostBroadcastHostedService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IVitalsBroadcaster _broadcaster;
    private readonly TimeProvider _clock;
    private readonly ILogger<SessionCostBroadcastHostedService> _logger;

    /// <summary>Creates the hosted service.</summary>
    public SessionCostBroadcastHostedService(
        IServiceScopeFactory scopeFactory,
        IVitalsBroadcaster broadcaster,
        TimeProvider clock,
        ILogger<SessionCostBroadcastHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _broadcaster = broadcaster;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TickInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await TickAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Session cost broadcast tick failed.");
            }
        }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDialysisSessionRepository>();
        var now = _clock.GetUtcNow().UtcDateTime;

        var sessions = await repository.ListActiveAsync(cancellationToken).ConfigureAwait(false);
        foreach (var session in sessions)
        {
            // Broadcast for paused sessions too: UsageMinutesAsOf freezes at the pause instant,
            // so the chairside cost holds steady (machine off) instead of going stale or ticking.
            if (session.ActualStartUtc is null
                || session.Status is not (DialysisSessionStatus.InProgress or DialysisSessionStatus.Paused))
                continue;

            // Pause-aware machine usage time — excludes paused spans so the live cost matches
            // the invoice EHR captures from the same DurationMinutes.
            var elapsedMinutes = session.UsageMinutesAsOf(now);
            var prescribedMinutes = Math.Max(1, session.Prescription.PrescribedDurationMinutes);
            var fraction = Math.Min(1m, elapsedMinutes / (decimal)prescribedMinutes);
            var estimatedUfLiters = Math.Round(
                session.Prescription.TargetUfVolumeLiters * fraction, 2, MidpointRounding.AwayFromZero);

            var breakdown = DialysisTariff.Compute("HD", elapsedMinutes, estimatedUfLiters);
            var snapshot = new SessionCostSnapshot(
                SessionId: session.Id,
                CurrencyCode: breakdown.CurrencyCode,
                Total: breakdown.Total,
                ElapsedMinutes: elapsedMinutes,
                AsOfUtc: now,
                Lines: breakdown.Lines
                    .Select(l => new SessionCostLineSnapshot(l.Label, l.Quantity, l.Unit, l.UnitPrice, l.Amount))
                    .ToList());

            await _broadcaster.BroadcastCostAsync(snapshot, cancellationToken).ConfigureAwait(false);
        }
    }
}
