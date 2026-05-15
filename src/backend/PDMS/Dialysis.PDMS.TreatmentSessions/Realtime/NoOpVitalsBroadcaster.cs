namespace Dialysis.PDMS.TreatmentSessions.Realtime;

/// <summary>Default no-op broadcaster used in unit tests and headless workers.</summary>
public sealed class NoOpVitalsBroadcaster : IVitalsBroadcaster
{
    public Task BroadcastAsync(VitalsReadingSnapshot reading, CancellationToken cancellationToken) => Task.CompletedTask;
}
