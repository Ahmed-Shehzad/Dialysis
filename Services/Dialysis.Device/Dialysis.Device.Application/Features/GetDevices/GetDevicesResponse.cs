namespace Dialysis.Device.Application.Features.GetDevices;

public sealed record GetDevicesResponse(IReadOnlyList<DeviceSummary> Devices);

public sealed record DeviceSummary(
    string Id,
    string DeviceEui64,
    string? Manufacturer,
    string? Model);
