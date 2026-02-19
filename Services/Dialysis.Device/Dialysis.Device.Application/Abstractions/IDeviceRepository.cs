using BuildingBlocks.Abstractions;

using DeviceDomain = Dialysis.Device.Application.Domain.Device;

namespace Dialysis.Device.Application.Abstractions;

public interface IDeviceRepository : IRepository<DeviceDomain>
{
    Task<DeviceDomain?> GetByIdAsync(Ulid id, CancellationToken cancellationToken = default);
    Task<DeviceDomain?> GetByDeviceEui64Async(string deviceEui64, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeviceDomain>> GetAllAsync(CancellationToken cancellationToken = default);
}
