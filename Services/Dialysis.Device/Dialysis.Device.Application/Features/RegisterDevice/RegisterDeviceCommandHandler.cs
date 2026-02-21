using BuildingBlocks.Caching;
using BuildingBlocks.Tenancy;

using Dialysis.Device.Application.Abstractions;
using Dialysis.Device.Application.Domain.ValueObjects;

using Intercessor.Abstractions;

using DeviceDomain = Dialysis.Device.Application.Domain.Device;

namespace Dialysis.Device.Application.Features.RegisterDevice;

internal sealed class RegisterDeviceCommandHandler : ICommandHandler<RegisterDeviceCommand, RegisterDeviceResponse>
{
    private const string DeviceKeyPrefix = "device";

    private readonly IDeviceRepository _repository;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly ITenantContext _tenant;

    public RegisterDeviceCommandHandler(IDeviceRepository repository, ICacheInvalidator cacheInvalidator, ITenantContext tenant)
    {
        _repository = repository;
        _cacheInvalidator = cacheInvalidator;
        _tenant = tenant;
    }

    public async Task<RegisterDeviceResponse> HandleAsync(RegisterDeviceCommand request, CancellationToken cancellationToken = default)
    {
        var deviceEui64 = new DeviceEui64(request.DeviceEui64);
        DeviceDomain? existing = await _repository.GetByDeviceEui64Async(deviceEui64, cancellationToken);
        if (existing is not null)
        {
            existing.UpdateDetails(request.Manufacturer, request.Model, request.Serial, request.Udi);
            _repository.Update(existing);
            await _repository.SaveChangesAsync(cancellationToken);
            await InvalidateDeviceCacheAsync(existing.Id.ToString(), existing.DeviceEui64.Value, cancellationToken);
            return new RegisterDeviceResponse(existing.Id.ToString(), Created: false);
        }

        var device = DeviceDomain.Register(
            deviceEui64,
            request.Manufacturer,
            request.Model,
            request.Serial,
            request.Udi,
            _tenant.TenantId);
        await _repository.AddAsync(device, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await InvalidateDeviceCacheAsync(device.Id.ToString(), device.DeviceEui64.Value, cancellationToken);
        return new RegisterDeviceResponse(device.Id.ToString(), Created: true);
    }

    private async Task InvalidateDeviceCacheAsync(string deviceId, string deviceEui64, CancellationToken cancellationToken)
    {
        string[] keys = new[] { $"{_tenant.TenantId}:{DeviceKeyPrefix}:id:{deviceId}", $"{_tenant.TenantId}:{DeviceKeyPrefix}:eui64:{deviceEui64}" };
        await _cacheInvalidator.InvalidateAsync(keys, cancellationToken);
    }
}
