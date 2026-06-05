using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;
using Dialysis.HIS.Integration.DeviceRegistry;

namespace Dialysis.HIS.Integration.Features.DeviceRegistry;

/// <summary>Lists registered devices (most-recently-registered first), capped by <see cref="Take"/>.</summary>
public sealed record ListDevicesQuery(int Take = 100) : IQuery<IReadOnlyList<DeviceSummaryDto>>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => HisPermissions.DeviceRead;
}

public sealed class ListDevicesQueryHandler : IQueryHandler<ListDevicesQuery, IReadOnlyList<DeviceSummaryDto>>
{
    private readonly IDeviceRepository _devices;
    public ListDevicesQueryHandler(IDeviceRepository devices) => _devices = devices;

    public async Task<IReadOnlyList<DeviceSummaryDto>> HandleAsync(
        ListDevicesQuery request, CancellationToken cancellationToken)
    {
        var take = request.Take is > 0 and <= 500 ? request.Take : 100;
        var devices = await _devices.ListAsync(take, cancellationToken).ConfigureAwait(false);
        return [.. devices.Select(DeviceProjections.ToSummary)];
    }
}
