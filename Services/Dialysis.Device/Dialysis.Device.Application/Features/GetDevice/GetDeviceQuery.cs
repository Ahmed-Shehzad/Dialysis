using Intercessor.Abstractions;

namespace Dialysis.Device.Application.Features.GetDevice;

public sealed record GetDeviceQuery(string DeviceId) : IQuery<GetDeviceResponse?>;
