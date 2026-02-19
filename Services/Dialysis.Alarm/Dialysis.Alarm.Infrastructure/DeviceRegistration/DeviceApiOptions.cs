namespace Dialysis.Alarm.Infrastructure.DeviceRegistration;

/// <summary>
/// Configuration for Device API integration (HL7 auto-registration).
/// </summary>
public sealed class DeviceApiOptions
{
    public const string SectionName = "DeviceApi";

    /// <summary>Base URL of the Device API (e.g. http://localhost:5054 or http://device-api:5054 in Docker).</summary>
    public string BaseUrl { get; set; } = "http://localhost:5054";

    /// <summary>When false, device registration is skipped (no-op).</summary>
    public bool Enabled { get; set; } = true;
}
