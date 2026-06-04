using Dialysis.Module.Contracts.Demo;
using Dialysis.PDMS.Persistence;
using Dialysis.PDMS.TreatmentSessions.Domain;
using Dialysis.PDMS.TreatmentSessions.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.PDMS.Composition.Demo;

/// <summary>
/// Development-only "autopilot" that continuously drives <em>its own</em> dialysis sessions
/// through the full lifecycle — schedule → start → (vitals via the ticker) → pause → resume →
/// complete — so the completion → billing → invoice → document pipeline fires for real on a
/// steady cadence (a fresh invoice + discharge/billing PDFs roughly once a minute).
///
/// It only ever loads sessions it created itself (tracked by id), so the snapshot the
/// <see cref="PdmsDemoSeeder"/> reserves for the presenter (the scheduled sessions) is never
/// touched. State changes go through the domain aggregate + unit of work — the same path the
/// command handlers use — so the raised integration events are harvested to the outbox and
/// relayed exactly as in production. (We can't use the CQRS gateway here: a background service
/// has no authenticated principal, and PDMS runs behind JWT in the Aspire stack.)
/// </summary>
public sealed class SessionLifecycleSimulator : BackgroundService
{
    // Consumers (EHR/HIE) bind their queues on startup; wait so the first completion's invoice
    // isn't published into an unbound exchange and dropped.
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(25);
    private static readonly TimeSpan Tick = TimeSpan.FromSeconds(8);

    private readonly IServiceProvider _services;
    private readonly TimeProvider _clock;
    private readonly ILogger<SessionLifecycleSimulator> _logger;

    private Phase _phase = Phase.Idle;
    private int _ticksInPhase;
    private int _patientCursor;
    private Guid? _sessionId;

    /// <summary>Creates the simulator.</summary>
    public SessionLifecycleSimulator(
        IServiceProvider services,
        TimeProvider clock,
        ILogger<SessionLifecycleSimulator> logger)
    {
        _services = services;
        _clock = clock;
        _logger = logger;
    }

    private enum Phase
    {
        Idle,       // about to schedule + start the next session
        Running,    // treatment in progress (vitals ticker fills readings)
        Paused,     // briefly paused to demo pause-aware accounting
        Resumed,    // running again before completion
        Cooldown,   // gap before the next patient
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(Tick);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await AdvanceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Session lifecycle simulator tick failed; continuing.");
                // Reset so a wedged session id doesn't stall the loop forever.
                _phase = Phase.Idle;
                _ticksInPhase = 0;
                _sessionId = null;
            }
        }
    }

    private async Task AdvanceAsync(CancellationToken ct)
    {
        _ticksInPhase++;
        switch (_phase)
        {
            case Phase.Idle:
                await StartNewSessionAsync(ct).ConfigureAwait(false);
                Transition(Phase.Running);
                break;

            // ~2 ticks running so the vitals ticker has populated the chart and the live cost has moved.
            case Phase.Running when _ticksInPhase >= 2:
                await MutateAsync(s => s.Pause(_clock.GetUtcNow().UtcDateTime), "paused", ct).ConfigureAwait(false);
                Transition(Phase.Paused);
                break;

            case Phase.Paused when _ticksInPhase >= 1:
                await MutateAsync(s => s.Resume(_clock.GetUtcNow().UtcDateTime), "resumed", ct).ConfigureAwait(false);
                Transition(Phase.Resumed);
                break;

            case Phase.Resumed when _ticksInPhase >= 2:
                await MutateAsync(
                    s => s.Complete(_clock.GetUtcNow().UtcDateTime, achievedUfVolumeLiters: 2.4m),
                    "completed", ct).ConfigureAwait(false);
                _sessionId = null;
                Transition(Phase.Cooldown);
                break;

            case Phase.Cooldown when _ticksInPhase >= 3:
                Transition(Phase.Idle);
                break;

            default:
                // Still waiting out the current phase.
                break;
        }
    }

    private void Transition(Phase next)
    {
        _phase = next;
        _ticksInPhase = 0;
    }

    private async Task StartNewSessionAsync(CancellationToken ct)
    {
        var patients = DemoDataCatalog.Patients;
        var patientId = patients[_patientCursor % patients.Count].Id;
        _patientCursor++;

        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IDialysisSessionRepository>();
        var db = scope.ServiceProvider.GetRequiredService<PdmsDbContext>();
        var now = _clock.GetUtcNow().UtcDateTime;

        var session = PdmsDemoSeeder.NewScheduledSession(patientId, now);
        session.Start(now);
        // A couple of readings so the chart isn't empty before the 1 Hz vitals ticker catches up.
        PdmsDemoSeeder.SeedReadings(session, now, count: 2, stepMinutes: 0.5);
        repo.Add(session);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        _sessionId = session.Id;
        _logger.LogDebug("Autopilot started session {SessionId} for patient {PatientId}.", session.Id, patientId);
    }

    private async Task MutateAsync(Action<DialysisSession> mutate, string verb, CancellationToken ct)
    {
        if (_sessionId is not { } id) return;

        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IDialysisSessionRepository>();
        var db = scope.ServiceProvider.GetRequiredService<PdmsDbContext>();

        var session = await repo.GetAsync(id, ct).ConfigureAwait(false);
        if (session is null) return;

        mutate(session);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogDebug("Autopilot {Verb} session {SessionId}.", verb, id);
    }
}
