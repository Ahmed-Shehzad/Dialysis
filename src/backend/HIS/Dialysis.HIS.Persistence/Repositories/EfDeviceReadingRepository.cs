using Dialysis.HIS.Integration.DeviceIngestion;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfDeviceReadingRepository : IDeviceReadingRepository
{
    private readonly HisDbContext _db;
    public EfDeviceReadingRepository(HisDbContext db) => _db = db;
    public void Add(DeviceReadingRecord record) => _db.DeviceReadings.Add(record);

    public Task<Guid?> FindIdByExternalMessageIdAsync(string externalMessageId, CancellationToken cancellationToken = default) =>
        _db.DeviceReadings.AsNoTracking()
            .Where(d => d.ExternalMessageId == externalMessageId)
            .Select(d => (Guid?)d.Id)
            .FirstOrDefaultAsync(cancellationToken);
}
