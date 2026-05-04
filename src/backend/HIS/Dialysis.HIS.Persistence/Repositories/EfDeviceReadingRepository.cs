using Dialysis.HIS.Integration.DeviceIngestion;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfDeviceReadingRepository(HisDbContext db) : IDeviceReadingRepository
{
    public void Add(DeviceReadingRecord record) => db.DeviceReadings.Add(record);

    public Task<Guid?> FindIdByExternalMessageIdAsync(string externalMessageId, CancellationToken cancellationToken = default) =>
        db.DeviceReadings.AsNoTracking()
            .Where(d => d.ExternalMessageId == externalMessageId)
            .Select(d => (Guid?)d.Id)
            .FirstOrDefaultAsync(cancellationToken);
}
