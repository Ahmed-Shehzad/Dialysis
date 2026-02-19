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
        IReadOnlyList<Domain.Device> devices = await _repository.GetAllAsync(cancellationToken);
        var summaries = devices.Select(d => new DeviceSummary(d.Id.ToString(), d.DeviceEui64, d.Manufacturer, d.Model)).ToList();
        return new GetDevicesResponse(summaries);
    }
}
