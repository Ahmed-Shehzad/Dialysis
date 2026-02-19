namespace Dialysis.Device.Api.Contracts;

public sealed record RegisterDeviceRequest(
    string DeviceEui64,
    string? Manufacturer = null,
    string? Model = null,
    string? Serial = null,
    string? Udi = null);
