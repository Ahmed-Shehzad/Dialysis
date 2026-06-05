using Dialysis.HIS.Integration.DeviceRegistry;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfDeviceRepository : IDeviceRepository
{
    private readonly HisDbContext _db;
    public EfDeviceRepository(HisDbContext db) => _db = db;

    public void Add(Device device) => _db.Devices.Add(device);

    public Task<Device?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Devices.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<Device?> FindByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default) =>
        _db.Devices.FirstOrDefaultAsync(d => d.DeviceId == deviceId, cancellationToken);

    public async Task<IReadOnlyList<Device>> ListAsync(int take, CancellationToken cancellationToken = default) =>
        await _db.Devices
            .AsNoTracking()
            .OrderByDescending(d => d.RegisteredAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
