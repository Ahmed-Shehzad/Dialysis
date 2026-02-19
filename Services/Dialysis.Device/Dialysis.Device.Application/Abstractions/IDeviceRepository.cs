using BuildingBlocks.Abstractions;

using DeviceDomain = Dialysis.Device.Application.Domain.Device;

namespace Dialysis.Device.Application.Abstractions;

public interface IDeviceRepository : IRepository<DeviceDomain>
{
    /// <summary>Used by RegisterDeviceCommandHandler to check for existing device (upsert).</summary>
    Task<DeviceDomain?> GetByDeviceEui64Async(string deviceEui64, CancellationToken cancellationToken = default);
}
