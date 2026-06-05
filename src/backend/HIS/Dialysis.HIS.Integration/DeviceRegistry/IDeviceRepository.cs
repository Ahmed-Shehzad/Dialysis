namespace Dialysis.HIS.Integration.DeviceRegistry;

/// <summary>Persistence port for the <see cref="Device"/> registry aggregate.</summary>
public interface IDeviceRepository
{
    void Add(Device device);

    Task<Device?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Find by the external device id — the key ingestion resolves a reading on.</summary>
    Task<Device?> FindByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Device>> ListAsync(int take, CancellationToken cancellationToken = default);
}
