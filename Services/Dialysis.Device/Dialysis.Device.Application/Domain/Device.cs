using BuildingBlocks;
using BuildingBlocks.ValueObjects;

using Dialysis.Device.Application.Domain.Events;
using Dialysis.Device.Application.Domain.ValueObjects;

namespace Dialysis.Device.Application.Domain;

/// <summary>
/// Dialysis machine device aggregate. Identity from MSH-3 (e.g. MACH^EUI64^EUI-64).
/// </summary>
public sealed class Device : AggregateRoot
{
    public TenantId TenantId { get; private set; }
    public DeviceEui64 DeviceEui64 { get; private set; }
    public string? Manufacturer { get; private set; }
    public string? Model { get; private set; }
    public string? Serial { get; private set; }
    public string? Udi { get; private set; }

    private Device() { }

    public static Device Register(DeviceEui64 deviceEui64, string? manufacturer = null, string? model = null, string? serial = null, string? udi = null, string? tenantId = null)
    {
        var device = new Device
        {
            TenantId = string.IsNullOrWhiteSpace(tenantId) ? BuildingBlocks.ValueObjects.TenantId.Default : new TenantId(tenantId),
            DeviceEui64 = deviceEui64,
            Manufacturer = manufacturer?.Trim(),
            Model = model?.Trim(),
            Serial = serial?.Trim(),
            Udi = udi?.Trim()
        };
        device.ApplyEvent(new DeviceRegisteredEvent(device.Id, device.DeviceEui64.Value, device.Manufacturer, device.Model));
        return device;
    }

    public void UpdateDetails(string? manufacturer, string? model, string? serial, string? udi)
    {
        if (manufacturer != null) Manufacturer = manufacturer.Trim();
        if (model != null) Model = model.Trim();
        if (serial != null) Serial = serial.Trim();
        if (udi != null) Udi = udi.Trim();
        ApplyUpdateDateTime();
        ApplyEvent(new DeviceDetailsUpdatedEvent(Id, DeviceEui64.Value, Manufacturer, Model, Serial, Udi));
    }
}
