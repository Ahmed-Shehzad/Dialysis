using System.Collections.Concurrent;
using Dialysis.HIS.PatientFlow.Domain;
using Dialysis.HIS.PatientFlow.Ports;
using Dialysis.Module.Contracts.Demo;

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

    public InMemoryPatientQueueRepository() => SeedToday();

    public IReadOnlyList<PatientQueueEntry> ListForToday(DateOnly today) =>
        [.. _entries.Values
            .Where(e => DateOnly.FromDateTime(e.ScheduledForUtc) == today)
            .OrderBy(e => e.ScheduledForUtc)];

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
            new(
                DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day,
                hour, minute, 0,
                DateTimeKind.Utc);

        // Seed today's queue from the cross-module DemoDataCatalog so the HIS board shows the same
        // patients EHR charts, PDMS treats and HIE shares — selecting a queue card carries that
        // patient's id (via PatientContext) into every other module's view.
        var patients = DemoDataCatalog.Patients;
        string Name(DemoPatient p) => $"{p.Given} {p.Family}";

        // Patients 0 & 1 are already in chairs (these two also have in-progress PDMS sessions).
        // Replay the state transitions so the entity passes through CheckIn / AssignChair rather
        // than being constructed mid-flow; the events raised along the way are historical, so
        // discard them before storing so the outbox isn't backfilled at startup.
        var inChairs = new[] { "Chair 4", "Chair 7" };
        for (var i = 0; i < 2; i++)
        {
            var p = patients[i];
            var entry = PatientQueueEntry.Schedule(Guid.NewGuid(), p.Id, Name(p), p.Mrn, At(7, 30), eligibilityVerified: true);
            entry.CheckIn(At(7, 30), eligibilityAcknowledged: false);
            entry.AssignChair(inChairs[i], At(7, 35 + i));
            entry.ClearIntegrationEvents();
            Stash(entry);
        }

        // Patient 2 already checked in, waiting for a chair (also has a PDMS session).
        var waiting = patients[2];
        var waitingEntry = PatientQueueEntry.Schedule(Guid.NewGuid(), waiting.Id, Name(waiting), waiting.Mrn, At(8, 45), eligibilityVerified: true);
        waitingEntry.CheckIn(At(8, 45), eligibilityAcknowledged: false);
        waitingEntry.ClearIntegrationEvents();
        Stash(waitingEntry);

        // Patients 3 & 4 are scheduled (expected) arrivals.
        Stash(PatientQueueEntry.Schedule(Guid.NewGuid(), patients[3].Id, Name(patients[3]), patients[3].Mrn, At(9, 0), eligibilityVerified: true));
        Stash(PatientQueueEntry.Schedule(Guid.NewGuid(), patients[4].Id, Name(patients[4]), patients[4].Mrn, At(9, 30), eligibilityVerified: false));
    }

    private void Stash(PatientQueueEntry entry) => _entries[entry.Id] = entry;
}
