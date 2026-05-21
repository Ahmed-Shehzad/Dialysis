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
/// is acceptable for the demo path. Once <c>dotnet ef migrations add HisPatientQueue</c>
/// has been run and <see cref="EfPatientQueueRepository"/> is registered instead, this
/// type becomes dead code and can be deleted.
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
        DateTime At(int hour, int minute) =>
            new DateTime(
                DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day,
                hour, minute, 0,
                DateTimeKind.Utc);

        Stash(PatientQueueEntry.Schedule(Guid.NewGuid(), Guid.NewGuid(),
            "Anna Müller", "MRN-10421", At(8, 0), eligibilityVerified: true));
        Stash(PatientQueueEntry.Schedule(Guid.NewGuid(), Guid.NewGuid(),
            "Erik Larsen", "MRN-10433", At(8, 30), eligibilityVerified: false));

        // A couple already checked in (replay the state transition so the entity passes
        // through CheckIn rather than being constructed in the Waiting state). The events
        // it raises along the way are historical — discard them before storing so the
        // outbox isn't backfilled at startup.
        var priya = PatientQueueEntry.Schedule(Guid.NewGuid(), Guid.NewGuid(),
            "Priya Shah", "MRN-10448", At(8, 45), eligibilityVerified: true);
        priya.CheckIn(At(8, 45), eligibilityAcknowledged: false);
        priya.ClearIntegrationEvents();
        Stash(priya);

        var liam = PatientQueueEntry.Schedule(Guid.NewGuid(), Guid.NewGuid(),
            "Liam O'Connor", "MRN-10455", At(8, 50), eligibilityVerified: true);
        liam.CheckIn(At(8, 50), eligibilityAcknowledged: false);
        liam.ClearIntegrationEvents();
        Stash(liam);

        // Two patients already in chairs.
        var sofia = PatientQueueEntry.Schedule(Guid.NewGuid(), Guid.NewGuid(),
            "Sofia Rossi", "MRN-10412", At(7, 30), eligibilityVerified: true);
        sofia.CheckIn(At(7, 30), eligibilityAcknowledged: false);
        sofia.AssignChair("Chair 4", At(7, 35));
        sofia.ClearIntegrationEvents();
        Stash(sofia);

        var henrik = PatientQueueEntry.Schedule(Guid.NewGuid(), Guid.NewGuid(),
            "Henrik Berg", "MRN-10401", At(7, 30), eligibilityVerified: true);
        henrik.CheckIn(At(7, 30), eligibilityAcknowledged: false);
        henrik.AssignChair("Chair 7", At(7, 36));
        henrik.ClearIntegrationEvents();
        Stash(henrik);
    }

    private void Stash(PatientQueueEntry entry) => _entries[entry.Id] = entry;
}
