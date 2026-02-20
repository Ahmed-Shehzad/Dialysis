using Refit;

namespace Dialysis.Treatment.Infrastructure.DeviceRegistration;

/// <summary>
/// Refit client for Device API (e.g. POST api/devices for HL7 auto-registration).
/// </summary>
internal interface IDeviceApi
{
    [Post("api/devices")]
    Task<ApiResponse<RegisterDeviceResponse>> RegisterAsync(
        [Body] RegisterDeviceRequest request,
        [Header("X-Tenant-Id")] string tenantId,
        CancellationToken cancellationToken = default);
}

internal sealed record RegisterDeviceRequest(string DeviceEui64, string? Manufacturer = null, string? Model = null, string? Serial = null, string? Udi = null);

internal sealed record RegisterDeviceResponse(string DeviceId, bool Created);
