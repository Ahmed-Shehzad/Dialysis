using Hl7.Fhir.Model;

namespace Dialysis.Hl7ToFhir;

/// <summary>
/// Maps dialysis machine identity (MSH-3, OBR-3) to FHIR Device.
/// </summary>
public static class DeviceMapper
{
    /// <summary>
    /// Create a minimal FHIR Device from device identifier (EUI-64 from MSH-3).
    /// </summary>
    public static Device ToFhirDevice(string deviceId, string? manufacturer = null, string? model = null)
    {
        return new Device
        {
            Identifier =
            [
                new Identifier("urn:ietf:rfc:3986", deviceId)
            ],
            Manufacturer = manufacturer,
            ModelNumber = model,
            Status = Device.FHIRDeviceStatus.Active
        };
    }
}
