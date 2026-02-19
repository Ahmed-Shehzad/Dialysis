using BuildingBlocks.Abstractions;

using Dialysis.Device.Application.Domain.ValueObjects;

using DeviceDomain = Dialysis.Device.Application.Domain.Device;

namespace Dialysis.Device.Application.Abstractions;

public interface IDeviceRepository : IRepository<DeviceDomain>
{
    /// <summary>Used by RegisterDeviceCommandHandler to check for existing device (upsert).</summary>
    Task<DeviceDomain?> GetByDeviceEui64Async(DeviceEui64 deviceEui64, CancellationToken cancellationToken = default);
}
