using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Contracts.Security;
using Dialysis.HIS.Integration.DeviceRegistry;

namespace Dialysis.HIS.Integration.Features.DeviceRegistry;

/// <summary>
/// Binds a registered device (by its registry id) to a patient and optional treatment session, so
/// the device's readings are attributed to the right patient. Re-binding moves the device; a
/// suspended/retired device cannot be bound.
/// </summary>
public sealed record BindDeviceToPatientCommand(
    Guid DeviceRegistryId,
    Guid PatientId,
    Guid? SessionId) : ICommand, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => HisPermissions.DeviceManage;
}

public sealed class BindDeviceToPatientCommandHandler : ICommandHandler<BindDeviceToPatientCommand, Unit>
{
    private readonly IDeviceRepository _devices;
    private readonly IUnitOfWork _unitOfWork;
    public BindDeviceToPatientCommandHandler(IDeviceRepository devices, IUnitOfWork unitOfWork)
    {
        _devices = devices;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> HandleAsync(BindDeviceToPatientCommand request, CancellationToken cancellationToken)
    {
        var device = await _devices.FindAsync(request.DeviceRegistryId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Device not found.");

        device.BindToPatient(request.PatientId, request.SessionId);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
