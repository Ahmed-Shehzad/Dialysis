using Dialysis.Device.Application.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Device.Application.Features.GetDevices;

internal sealed class GetDevicesQueryHandler : IQueryHandler<GetDevicesQuery, GetDevicesResponse>
{
    private readonly IDeviceRepository _repository;

    public GetDevicesQueryHandler(IDeviceRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetDevicesResponse> HandleAsync(GetDevicesQuery request, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Domain.Device> devices;
        if (!string.IsNullOrWhiteSpace(request.Id) && Ulid.TryParse(request.Id, out Ulid id))
        {
            Domain.Device? d = await _repository.GetByIdAsync(id, cancellationToken);
            devices = d is not null ? [d] : [];
        }
        else if (!string.IsNullOrWhiteSpace(request.Identifier))
        {
            Domain.Device? byEui = await _repository.GetByDeviceEui64Async(request.Identifier.Trim(), cancellationToken);
            if (byEui is not null)
                devices = [byEui];
            else if (Ulid.TryParse(request.Identifier, out Ulid id2))
            {
                Domain.Device? d = await _repository.GetByIdAsync(id2, cancellationToken);
                devices = d is not null ? [d] : [];
            }
            else
                devices = [];
        }
        else devices = await _repository.GetAllAsync(cancellationToken);

        var summaries = devices.Select(d => new DeviceSummary(d.Id.ToString(), d.DeviceEui64, d.Manufacturer, d.Model)).ToList();
        return new GetDevicesResponse(summaries);
    }
}
