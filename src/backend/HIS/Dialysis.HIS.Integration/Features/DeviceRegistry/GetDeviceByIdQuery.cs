using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;
using Dialysis.HIS.Integration.DeviceRegistry;

namespace Dialysis.HIS.Integration.Features.DeviceRegistry;

/// <summary>Reads one registered device by its registry id.</summary>
public sealed record GetDeviceByIdQuery(Guid Id) : IQuery<DeviceDto?>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => HisPermissions.DeviceRead;
}

public sealed class GetDeviceByIdQueryHandler : IQueryHandler<GetDeviceByIdQuery, DeviceDto?>
{
    private readonly IDeviceRepository _devices;
    public GetDeviceByIdQueryHandler(IDeviceRepository devices) => _devices = devices;

    public async Task<DeviceDto?> HandleAsync(GetDeviceByIdQuery request, CancellationToken cancellationToken)
    {
        var device = await _devices.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        return device is null ? null : DeviceProjections.ToDto(device);
    }
}
