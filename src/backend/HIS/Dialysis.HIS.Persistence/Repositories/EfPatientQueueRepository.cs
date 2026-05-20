using Dialysis.HIS.PatientFlow.Domain;
using Dialysis.HIS.PatientFlow.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IPatientQueueRepository"/>. Queries are
/// tracking by default so handler mutations are picked up by `SaveChangesAsync`;
/// `ListForToday` uses no-tracking because it is a read-only projection.
/// </summary>
public sealed class EfPatientQueueRepository(HisDbContext db) : IPatientQueueRepository
{
    public IReadOnlyList<PatientQueueEntry> ListForToday(DateOnly today)
    {
        // Postgres has no `date == @dt::date` shortcut from the .NET side; do the date
        // arithmetic in C# by computing the day's UTC bounds and filtering with a range.
        var startUtc = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc = startUtc.AddDays(1);
        return db.PatientQueueEntries
            .AsNoTracking()
            .Where(e => e.ScheduledForUtc >= startUtc && e.ScheduledForUtc < endUtc)
            .OrderBy(e => e.ScheduledForUtc)
            .ToList();
    }

    public PatientQueueEntry? Get(Guid id) =>
        // Tracked: handlers mutate the entity, `SaveChangesAsync` persists.
        db.PatientQueueEntries.FirstOrDefault(e => e.Id == id);

    public void Add(PatientQueueEntry entry) => db.PatientQueueEntries.Add(entry);

    public bool IsChairOccupied(DateOnly today, string chair)
    {
        var startUtc = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc = startUtc.AddDays(1);
        return db.PatientQueueEntries
            .AsNoTracking()
            .Any(e =>
                e.ScheduledForUtc >= startUtc &&
                e.ScheduledForUtc < endUtc &&
                e.Status == QueueStatus.InTreatment &&
                e.Chair == chair);
    }
}
