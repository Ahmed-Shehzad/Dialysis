using Intercessor.Abstractions;

namespace Dialysis.Device.Application.Features.RegisterDevice;

public sealed record RegisterDeviceCommand(
    string DeviceEui64,
    string? Manufacturer = null,
    string? Model = null,
    string? Serial = null,
    string? Udi = null) : ICommand<RegisterDeviceResponse>;
