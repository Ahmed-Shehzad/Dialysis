using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Integration.DeviceRegistry;

namespace Dialysis.HIS.Integration.Features.DeviceRegistry;

public sealed class RegisterDeviceCommandHandler : ICommandHandler<RegisterDeviceCommand, Guid>
{
    private readonly IDeviceRepository _devices;
    private readonly IDeviceTypeCatalog _catalog;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterDeviceCommandHandler(
        IDeviceRepository devices,
        IDeviceTypeCatalog catalog,
        IUnitOfWork unitOfWork)
    {
        _devices = devices;
        _catalog = catalog;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> HandleAsync(RegisterDeviceCommand request, CancellationToken cancellationToken)
    {
        if (_catalog.Find(request.DeviceTypeCode) is null)
            throw new DomainException($"Unknown device type '{request.DeviceTypeCode}'.");

        var existing = await _devices
            .FindByDeviceIdAsync(request.DeviceId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
            throw new DomainException($"A device with id '{request.DeviceId}' is already registered.");

        var device = Device.Register(
            request.DeviceId,
            request.DeviceTypeCode,
            request.Manufacturer,
            request.Model,
            request.SerialNumber,
            request.CalibrationDueUtc,
            DateTime.UtcNow);

        _devices.Add(device);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return device.Id;
    }
}
