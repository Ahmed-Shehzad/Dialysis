namespace Dialysis.SmartConnect.Inbound.Hosting;

/// <summary>
/// Configuration for one source-connector instance under <c>SmartConnect:SourceConnectors:[]</c>.
/// </summary>
public sealed class SourceConnectorInstanceOptions
{
    /// <summary>Operator-friendly identifier, used in logs.</summary>
    public string Name { get; set; } = "";

    /// <summary>Connector kind (e.g. <c>"file-reader"</c>); resolved against <see cref="ISourceConnectorRegistry"/>.</summary>
    public string Kind { get; set; } = "";

    /// <summary>Default <see cref="IntegrationMessage.FlowId"/> for messages dispatched by this instance.</summary>
    public Guid DefaultFlowId { get; set; }

    /// <summary>Whether the host should start this instance (default true).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Kind-specific configuration values (case-insensitive).</summary>
    public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Root options bound from <c>SmartConnect:SourceConnectors</c>.</summary>
public sealed class SourceConnectorHostOptions
{
    public List<SourceConnectorInstanceOptions> Instances { get; set; } = [];
}
