using Dialysis.Device.Application.Abstractions;
using Dialysis.Device.Infrastructure.Persistence;
using Dialysis.Device.Infrastructure.ReadModels;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Device.Infrastructure;

public sealed class DeviceReadStore : IDeviceReadStore
{
    private readonly DeviceReadDbContext _db;

    public DeviceReadStore(DeviceReadDbContext db)
    {
        _db = db;
    }

    public async Task<DeviceReadDto?> GetByIdAsync(string tenantId, string deviceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId)) return null;
        DeviceReadModel? m = await _db.Devices
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Id == deviceId, cancellationToken);
        return m is null ? null : ToDto(m);
    }

    public async Task<DeviceReadDto?> GetByDeviceEui64Async(string tenantId, string deviceEui64, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceEui64)) return null;
        DeviceReadModel? m = await _db.Devices
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.DeviceEui64 == deviceEui64, cancellationToken);
        return m is null ? null : ToDto(m);
    }

    public async Task<IReadOnlyList<DeviceReadDto>> GetAllForTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        List<DeviceReadModel> list = await _db.Devices
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId)
            .OrderBy(d => d.DeviceEui64)
            .ToListAsync(cancellationToken);
        return list.Select(ToDto).ToList();
    }

    private static DeviceReadDto ToDto(DeviceReadModel m) =>
        new(m.Id, m.DeviceEui64, m.Manufacturer, m.Model, m.Serial, m.Udi);
}
