using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Integration.Features.DeviceRegistry;

/// <summary>
/// Registers a device in the RPM device registry. The declared <see cref="DeviceTypeCode"/> must
/// resolve in the configured device-type catalog; the <see cref="DeviceId"/> (the external
/// serial/gateway id the device stamps on its readings) must be unique. Returns the new registry id.
/// </summary>
public sealed record RegisterDeviceCommand(
    string DeviceId,
    string DeviceTypeCode,
    string? Manufacturer,
    string? Model,
    string? SerialNumber,
    DateTime? CalibrationDueUtc) : ICommand<Guid>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => HisPermissions.DeviceRegister;
}
