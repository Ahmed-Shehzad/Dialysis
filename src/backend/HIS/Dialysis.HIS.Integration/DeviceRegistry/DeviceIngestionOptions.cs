namespace Dialysis.HIS.Integration.DeviceRegistry;

/// <summary>
/// Policy for how device-reading ingestion interacts with the device registry. Bound from
/// <c>His:DeviceRegistry</c>. Defaults keep the pre-registry behaviour so the registry can be
/// rolled out without breaking existing ingest paths.
/// </summary>
public sealed class DeviceIngestionOptions
{
    /// <summary>Configuration section these options bind from.</summary>
    public const string SectionName = "His:DeviceRegistry";

    /// <summary>
    /// When true, a reading whose <c>DeviceId</c> has no registry entry is rejected. When false
    /// (default), unknown devices are accepted as before — but a reading from a <em>registered</em>
    /// device is always governed (status checked, patient binding enforced, last-seen stamped).
    /// </summary>
    public bool RequireRegistration { get; set; }
}
