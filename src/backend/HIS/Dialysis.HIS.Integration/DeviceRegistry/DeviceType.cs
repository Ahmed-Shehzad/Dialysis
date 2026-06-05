namespace Dialysis.HIS.Integration.DeviceRegistry;

/// <summary>
/// One registered class of remote-patient-monitoring device. The catalog is intentionally
/// data-driven (seeded from configuration) so a new device class — pulse-oximeter, weight scale,
/// glucose meter, blood-pressure cuff — is a config entry, not a code change. <see cref="Category"/>
/// groups physical device families; <see cref="Unit"/> is the canonical measurement unit a reading
/// from this class is expected to carry (informational, for downstream display/validation).
/// </summary>
public sealed record DeviceType(string Code, string Display, string Category, string? Unit = null);

/// <summary>
/// Lookup over the configured set of <see cref="DeviceType"/>s. Registration validates a device's
/// declared type against this catalog so an unknown class can't enter the registry.
/// </summary>
public interface IDeviceTypeCatalog
{
    /// <summary>All known device types, ordered by code.</summary>
    IReadOnlyCollection<DeviceType> All { get; }

    /// <summary>Resolves a device type by its code (case-insensitive); null when unknown.</summary>
    DeviceType? Find(string code);
}
