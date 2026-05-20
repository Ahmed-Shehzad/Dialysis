using System.Collections.Concurrent;
using Dialysis.HIS.PatientFlow.Domain;
using Dialysis.HIS.PatientFlow.Ports;

namespace Dialysis.HIS.Persistence.Repositories;

/// <summary>
/// In-memory store for today's patient queue, seeded with demo entries on construction so
/// the receptionist flow demos end-to-end without a full backing EF model.
/// </summary>
/// <remarks>
/// Singleton-scoped — every host shares one queue. Concurrent dictionary keeps reads
/// allocation-free. Per-instance: bring up multiple replicas and they will diverge; that
/// is acceptable for the demo path. Swap for an EF-backed repo once the HIS Scheduling +
/// PatientFlow slices commit to a persisted query shape.
/// </remarks>
public sealed class InMemoryPatientQueueRepository : IPatientQueueRepository
{
    private readonly ConcurrentDictionary<Guid, PatientQueueEntry> _entries = new();

    public InMemoryPatientQueueRepository()
    {
        SeedToday();
    }

    public IReadOnlyList<PatientQueueEntry> ListForToday(DateOnly today) =>
        _entries.Values
            .Where(e => DateOnly.FromDateTime(e.ScheduledForUtc) == today)
            .OrderBy(e => e.ScheduledForUtc)
            .ToList();

    public PatientQueueEntry? Get(Guid id) =>
        _entries.TryGetValue(id, out var entry) ? entry : null;

    public void Add(PatientQueueEntry entry) => _entries[entry.Id] = entry;

    public bool IsChairOccupied(DateOnly today, string chair) =>
        _entries.Values.Any(e =>
            DateOnly.FromDateTime(e.ScheduledForUtc) == today &&
            e.Status == QueueStatus.InTreatment &&
            string.Equals(e.Chair, chair, StringComparison.OrdinalIgnoreCase));

    private void SeedToday()
    {
        // Today's date with fixed clinic-day times; UTC for storage.
        DateTime At(int hour, int minute) =>
            new DateTime(
                DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day,
                hour, minute, 0,
                DateTimeKind.Utc);

        Add(PatientQueueEntry.Schedule(Guid.NewGuid(), Guid.NewGuid(),
            "Anna Müller", "MRN-10421", At(8, 0), eligibilityVerified: true));
        Add(PatientQueueEntry.Schedule(Guid.NewGuid(), Guid.NewGuid(),
            "Erik Larsen", "MRN-10433", At(8, 30), eligibilityVerified: false));

        // A couple already checked in.
        var priya = PatientQueueEntry.Schedule(Guid.NewGuid(), Guid.NewGuid(),
            "Priya Shah", "MRN-10448", At(8, 45), eligibilityVerified: true);
        priya.CheckIn(eligibilityAcknowledged: false);
        Add(priya);

        var liam = PatientQueueEntry.Schedule(Guid.NewGuid(), Guid.NewGuid(),
            "Liam O'Connor", "MRN-10455", At(8, 50), eligibilityVerified: true);
        liam.CheckIn(eligibilityAcknowledged: false);
        Add(liam);

        // Two patients already in chairs.
        var sofia = PatientQueueEntry.Schedule(Guid.NewGuid(), Guid.NewGuid(),
            "Sofia Rossi", "MRN-10412", At(7, 30), eligibilityVerified: true);
        sofia.CheckIn(eligibilityAcknowledged: false);
        sofia.AssignChair("Chair 4");
        Add(sofia);

        var henrik = PatientQueueEntry.Schedule(Guid.NewGuid(), Guid.NewGuid(),
            "Henrik Berg", "MRN-10401", At(7, 30), eligibilityVerified: true);
        henrik.CheckIn(eligibilityAcknowledged: false);
        henrik.AssignChair("Chair 7");
        Add(henrik);
    }
}
