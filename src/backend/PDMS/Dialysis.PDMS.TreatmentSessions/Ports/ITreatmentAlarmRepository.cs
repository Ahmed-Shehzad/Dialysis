using Dialysis.PDMS.TreatmentSessions.Domain;

namespace Dialysis.PDMS.TreatmentSessions.Ports;

/// <summary>
/// Read/write port for <see cref="TreatmentAlarm"/> aggregates. Backed by EF Core in
/// production; the in-memory provider used by tests creates the table on
/// <c>EnsureCreatedAsync</c> from the existing mapping in <c>PdmsDbContext</c>.
/// </summary>
public interface ITreatmentAlarmRepository
{
    void Add(TreatmentAlarm alarm);

    /// <summary>
    /// Loads a tracked <see cref="TreatmentAlarm"/> by id for state mutation (acknowledge,
    /// future operator actions). Returns <c>null</c> when the alarm does not exist.
    /// </summary>
    Task<TreatmentAlarm?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks up the live (non-Resolved) alarm for a given machine + code if one is open,
    /// so the consumer can route the incoming state-change to the same aggregate instead
    /// of raising a duplicate.
    /// </summary>
    Task<TreatmentAlarm?> FindLiveAsync(Guid machineId, long alarmCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns alarms currently <c>Present</c> or <c>Inactivating</c>, ordered by first observation.
    /// The chairside strip is the primary consumer.
    /// </summary>
    Task<IReadOnlyList<TreatmentAlarm>> ListActiveAsync(CancellationToken cancellationToken = default);
}
