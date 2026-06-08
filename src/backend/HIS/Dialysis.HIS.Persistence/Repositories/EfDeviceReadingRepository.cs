using Dialysis.HIS.Integration.DeviceIngestion;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfDeviceReadingRepository : IDeviceReadingRepository
{
    private readonly HisDbContext _db;
    public EfDeviceReadingRepository(HisDbContext db) => _db = db;

    public Task<Guid?> FindIdByExternalMessageIdAsync(string externalMessageId, CancellationToken cancellationToken = default) =>
        _db.DeviceReadings.AsNoTracking()
            .Where(d => d.ExternalMessageId == externalMessageId)
            .Select(d => (Guid?)d.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Guid> PersistIdempotentAsync(DeviceReadingRecord record, CancellationToken cancellationToken = default)
    {
        _db.DeviceReadings.Add(record);
        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return record.Id;
        }
        catch (DbUpdateException) when (!string.IsNullOrWhiteSpace(record.ExternalMessageId))
        {
            // A concurrent ingest inserted the same ExternalMessageId between the caller's dedup read
            // and this save, so the unique index rejected ours. Detach the doomed insert and return
            // the winner's id — re-ingest stays idempotent instead of 500-ing on the race.
            //
            // Under an ambient transaction (the durable command consumer) the failed statement aborts
            // the transaction, so the re-read below throws and the command is rolled back + redelivered;
            // by the retry the winner has committed and the caller's fast-path dedup read resolves it.
            _db.Entry(record).State = EntityState.Detached;
            var existing = await FindIdByExternalMessageIdAsync(record.ExternalMessageId!, cancellationToken)
                .ConfigureAwait(false);
            if (existing is { } winner)
                return winner;
            throw;
        }
    }
}
