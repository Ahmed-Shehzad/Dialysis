using BuildingBlocks.Tenancy;

using Dialysis.Device.Application.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Device.Application.Features.GetDevices;

internal sealed class GetDevicesQueryHandler : IQueryHandler<GetDevicesQuery, GetDevicesResponse>
{
    private readonly IDeviceReadStore _readStore;
    private readonly ITenantContext _tenant;

    public GetDevicesQueryHandler(IDeviceReadStore readStore, ITenantContext tenant)
    {
        _readStore = readStore;
        _tenant = tenant;
    }

    public async Task<GetDevicesResponse> HandleAsync(GetDevicesQuery request, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DeviceReadDto> devices;
        if (!string.IsNullOrWhiteSpace(request.Id))
        {
            DeviceReadDto? d = await _readStore.GetByIdAsync(_tenant.TenantId, request.Id, cancellationToken);
            devices = d is not null ? [d] : [];
        }
        else if (!string.IsNullOrWhiteSpace(request.Identifier))
        {
            string id = request.Identifier.Trim();
            DeviceReadDto? byEui = await _readStore.GetByDeviceEui64Async(_tenant.TenantId, id, cancellationToken);
            if (byEui is not null)
                devices = [byEui];
            else
            {
                DeviceReadDto? byId = await _readStore.GetByIdAsync(_tenant.TenantId, id, cancellationToken);
                devices = byId is not null ? [byId] : [];
            }
        }
        else
            devices = await _readStore.GetAllForTenantAsync(_tenant.TenantId, cancellationToken);

        var summaries = devices.Select(d => new DeviceSummary(d.Id, d.DeviceEui64, d.Manufacturer, d.Model)).ToList();
        return new GetDevicesResponse(summaries);
    }
}
