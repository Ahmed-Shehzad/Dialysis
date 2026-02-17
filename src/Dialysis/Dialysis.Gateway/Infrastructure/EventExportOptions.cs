namespace Dialysis.Gateway.Infrastructure;

/// <summary>
/// Configuration for event export to Azure Service Bus via Transponder.
/// </summary>
public sealed class EventExportOptions
{
    public const string Section = "EventExport";

    /// <summary>
    /// When true, events are published to Azure Service Bus via Transponder.
    /// </summary>
    public bool UseAzureServiceBus { get; set; }

    /// <summary>
    /// Azure Service Bus connection string. Required when UseAzureServiceBus is true.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Topic name for event export messages (default: dialysis-events).
    /// </summary>
    public string Topic { get; set; } = "dialysis-events";

    public bool IsConfigured =>
        UseAzureServiceBus && !string.IsNullOrWhiteSpace(ConnectionString);
}
