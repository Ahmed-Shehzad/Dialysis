namespace Dialysis.SmartConnect.Alerts;

/// <summary>
/// One configured action attached to an <see cref="AlertRule"/>. <see cref="Kind"/> resolves to an
/// <see cref="IAlertActionProvider"/> via the plugin registry; <see cref="PropertiesJson"/> is the
/// provider-specific configuration (e.g. SMTP host, webhook URL, target flow Id).
/// </summary>
public sealed class AlertActionSlot
{
    public string Kind { get; set; } = "";

    public string? PropertiesJson { get; set; }
}
