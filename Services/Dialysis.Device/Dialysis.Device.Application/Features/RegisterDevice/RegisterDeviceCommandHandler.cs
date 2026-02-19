using BuildingBlocks.Tenancy;

using Dialysis.Device.Application.Abstractions;

using Intercessor.Abstractions;

using DeviceDomain = Dialysis.Device.Application.Domain.Device;

namespace Dialysis.Device.Application.Features.RegisterDevice;

internal sealed class RegisterDeviceCommandHandler : ICommandHandler<RegisterDeviceCommand, RegisterDeviceResponse>
{
    private readonly IDeviceRepository _repository;
    private readonly ITenantContext _tenant;

    public RegisterDeviceCommandHandler(IDeviceRepository repository, ITenantContext tenant)
    {
        _repository = repository;
        _tenant = tenant;
    }

    public async Task<RegisterDeviceResponse> HandleAsync(RegisterDeviceCommand request, CancellationToken cancellationToken = default)
    {
        DeviceDomain? existing = await _repository.GetByDeviceEui64Async(request.DeviceEui64, cancellationToken);
        if (existing is not null)
        {
            existing.UpdateDetails(request.Manufacturer, request.Model, request.Serial, request.Udi);
            _repository.Update(existing);
            await _repository.SaveChangesAsync(cancellationToken);
            return new RegisterDeviceResponse(existing.Id.ToString(), Created: false);
        }

        var device = DeviceDomain.Register(
            request.DeviceEui64,
            request.Manufacturer,
            request.Model,
            request.Serial,
            request.Udi,
            _tenant.TenantId);
        await _repository.AddAsync(device, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return new RegisterDeviceResponse(device.Id.ToString(), Created: true);
    }
}
