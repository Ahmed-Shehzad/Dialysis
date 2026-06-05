using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Contracts.Security;
using Dialysis.HIS.Integration.DeviceRegistry;

namespace Dialysis.HIS.Integration.Features.DeviceRegistry;

/// <summary>Lifecycle transitions a steward can apply to a registered device.</summary>
public enum DeviceStatusAction
{
    /// <summary>Block the device from reporting (reversible).</summary>
    Suspend = 0,

    /// <summary>Re-enable a suspended device.</summary>
    Activate = 1,

    /// <summary>Permanently decommission the device.</summary>
    Retire = 2,
}

/// <summary>Applies a lifecycle transition (suspend / activate / retire) to a registered device.</summary>
public sealed record ChangeDeviceStatusCommand(
    Guid DeviceRegistryId,
    DeviceStatusAction Action) : ICommand, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => HisPermissions.DeviceManage;
}

public sealed class ChangeDeviceStatusCommandHandler : ICommandHandler<ChangeDeviceStatusCommand, Unit>
{
    private readonly IDeviceRepository _devices;
    private readonly IUnitOfWork _unitOfWork;
    public ChangeDeviceStatusCommandHandler(IDeviceRepository devices, IUnitOfWork unitOfWork)
    {
        _devices = devices;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> HandleAsync(ChangeDeviceStatusCommand request, CancellationToken cancellationToken)
    {
        var device = await _devices.FindAsync(request.DeviceRegistryId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Device not found.");

        switch (request.Action)
        {
            case DeviceStatusAction.Suspend:
                device.Suspend();
                break;
            case DeviceStatusAction.Activate:
                device.Activate();
                break;
            case DeviceStatusAction.Retire:
                device.Retire();
                break;
            default:
                throw new DomainException($"Unknown device status action '{request.Action}'.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
