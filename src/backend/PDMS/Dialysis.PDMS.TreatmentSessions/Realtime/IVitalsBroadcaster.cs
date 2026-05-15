namespace Dialysis.PDMS.TreatmentSessions.Realtime;

public sealed record VitalsReadingSnapshot(
    Guid ReadingId,
    Guid SessionId,
    DateTime ObservedAtUtc,
    int SystolicBloodPressure,
    int DiastolicBloodPressure,
    int HeartRateBpm,
    decimal ArterialPressureMmHg,
    decimal VenousPressureMmHg,
    decimal UltrafiltrationRateMlPerHour,
    decimal ConductivityMsPerCm,
    string? Notes);

public interface IVitalsBroadcaster
{
    Task BroadcastAsync(VitalsReadingSnapshot reading, CancellationToken cancellationToken);
}
