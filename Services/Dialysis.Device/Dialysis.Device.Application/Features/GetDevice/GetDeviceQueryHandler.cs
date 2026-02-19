using Dialysis.Device.Application.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Device.Application.Features.GetDevice;

internal sealed class GetDeviceQueryHandler : IQueryHandler<GetDeviceQuery, GetDeviceResponse?>
{
    private readonly IDeviceRepository _repository;

    public GetDeviceQueryHandler(IDeviceRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetDeviceResponse?> HandleAsync(GetDeviceQuery request, CancellationToken cancellationToken = default)
    {
        Domain.Device? device;
        if (Ulid.TryParse(request.DeviceId, out Ulid id))
            device = await _repository.GetByIdAsync(id, cancellationToken);
        else
            device = await _repository.GetByDeviceEui64Async(request.DeviceId, cancellationToken);

        return device is null ? null : new GetDeviceResponse(
            device.Id.ToString(),
            device.DeviceEui64,
            device.Manufacturer,
            device.Model,
            device.Serial,
            device.Udi);
    }
}
