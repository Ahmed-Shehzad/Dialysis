using BuildingBlocks;
using BuildingBlocks.Tenancy;

using Dialysis.Device.Application.Domain.Events;

namespace Dialysis.Device.Application.Domain;

/// <summary>
/// Dialysis machine device aggregate. Identity from MSH-3 (e.g. MACH^EUI64^EUI-64).
/// </summary>
public sealed class Device : AggregateRoot
{
    public string TenantId { get; private set; } = TenantContext.DefaultTenantId;
    public string DeviceEui64 { get; private set; } = string.Empty;
    public string? Manufacturer { get; private set; }
    public string? Model { get; private set; }
    public string? Serial { get; private set; }
    public string? Udi { get; private set; }

    private Device() { }

    public static Device Register(string deviceEui64, string? manufacturer = null, string? model = null, string? serial = null, string? udi = null, string? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceEui64);
        var device = new Device
        {
            TenantId = string.IsNullOrWhiteSpace(tenantId) ? TenantContext.DefaultTenantId : tenantId,
            DeviceEui64 = deviceEui64.Trim(),
            Manufacturer = manufacturer?.Trim(),
            Model = model?.Trim(),
            Serial = serial?.Trim(),
            Udi = udi?.Trim()
        };
        device.ApplyEvent(new DeviceRegisteredEvent(device.Id, device.DeviceEui64, device.Manufacturer, device.Model));
        return device;
    }

    public void UpdateDetails(string? manufacturer, string? model, string? serial, string? udi)
    {
        if (manufacturer != null) Manufacturer = manufacturer.Trim();
        if (model != null) Model = model.Trim();
        if (serial != null) Serial = serial.Trim();
        if (udi != null) Udi = udi.Trim();
        ApplyUpdateDateTime();
        ApplyEvent(new DeviceDetailsUpdatedEvent(Id, DeviceEui64, Manufacturer, Model, Serial, Udi));
    }
}
