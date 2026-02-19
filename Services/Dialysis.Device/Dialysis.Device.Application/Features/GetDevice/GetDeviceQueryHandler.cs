using BuildingBlocks.Tenancy;

using Dialysis.Device.Application.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Device.Application.Features.GetDevice;

internal sealed class GetDeviceQueryHandler : IQueryHandler<GetDeviceQuery, GetDeviceResponse?>
{
    private readonly IDeviceReadStore _readStore;
    private readonly ITenantContext _tenant;

    public GetDeviceQueryHandler(IDeviceReadStore readStore, ITenantContext tenant)
    {
        _readStore = readStore;
        _tenant = tenant;
    }

    public async Task<GetDeviceResponse?> HandleAsync(GetDeviceQuery request, CancellationToken cancellationToken = default)
    {
        DeviceReadDto? dto = Ulid.TryParse(request.DeviceId, out _)
            ? await _readStore.GetByIdAsync(_tenant.TenantId, request.DeviceId, cancellationToken)
            : await _readStore.GetByDeviceEui64Async(_tenant.TenantId, request.DeviceId, cancellationToken);
        return dto is null ? null : new GetDeviceResponse(dto.Id, dto.DeviceEui64, dto.Manufacturer, dto.Model, dto.Serial, dto.Udi);
    }
}
