namespace Dialysis.Device.Application.Features.GetDevice;

public sealed record GetDeviceResponse(
    string Id,
    string DeviceEui64,
    string? Manufacturer,
    string? Model,
    string? Serial,
    string? Udi);
