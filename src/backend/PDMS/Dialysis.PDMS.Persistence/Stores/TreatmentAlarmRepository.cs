using Dialysis.PDMS.TreatmentSessions.Domain;
using Dialysis.PDMS.TreatmentSessions.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.PDMS.Persistence.Stores;

public sealed class TreatmentAlarmRepository(PdmsDbContext db) : ITreatmentAlarmRepository
{
    public void Add(TreatmentAlarm alarm) => db.TreatmentAlarms.Add(alarm);

    public Task<TreatmentAlarm?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.TreatmentAlarms.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<TreatmentAlarm?> FindLiveAsync(Guid machineId, long alarmCode, CancellationToken cancellationToken = default) =>
        db.TreatmentAlarms
            .Where(a =>
                a.MachineId == machineId &&
                a.AlarmCode == alarmCode &&
                a.State != TreatmentAlarmState.Resolved)
            .OrderByDescending(a => a.FirstObservedUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<TreatmentAlarm>> ListActiveAsync(CancellationToken cancellationToken = default) =>
        await db.TreatmentAlarms
            .AsNoTracking()
            .Where(a => a.State != TreatmentAlarmState.Resolved)
            .OrderBy(a => a.FirstObservedUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
