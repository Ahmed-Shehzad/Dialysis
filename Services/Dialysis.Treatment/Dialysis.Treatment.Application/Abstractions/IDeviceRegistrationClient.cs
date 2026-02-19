namespace Dialysis.Treatment.Application.Abstractions;

/// <summary>
/// Ensures a dialysis device is registered in the Device service when first seen via HL7.
/// Called from ORU^R01 ingest when DeviceEui64 is present.
/// </summary>
public interface IDeviceRegistrationClient
{
    /// <summary>
    /// Register or update the device in the Device catalog. Best-effort; failures are logged and do not block ingestion.
    /// </summary>
    Task EnsureRegisteredAsync(string deviceEui64, CancellationToken cancellationToken = default);
}
