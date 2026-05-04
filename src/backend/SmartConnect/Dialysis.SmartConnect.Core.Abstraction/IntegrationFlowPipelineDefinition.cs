namespace Dialysis.SmartConnect;

/// <summary>
/// Serializable pipeline for an <see cref="IntegrationFlow"/> (filters, then parallel outbound routes).
/// </summary>
public sealed class IntegrationFlowPipelineDefinition
{
    public List<RouteFilterSlot> RouteFilters { get; set; } = [];

    public List<OutboundRouteSlot> OutboundRoutes { get; set; } = [];
}

public sealed class RouteFilterSlot
{
    public string Kind { get; set; } = "";

    public string? ParametersJson { get; set; }
}

public sealed class OutboundRouteSlot
{
    public string OutboundAdapterKind { get; set; } = "";

    /// <summary>Optional JSON consumed by the outbound adapter (e.g. URL, path, SMTP settings).</summary>
    public string? OutboundParametersJson { get; set; }

    /// <summary>Minimum 1. Retries failed sends with backoff between attempts.</summary>
    public int MaxAttempts { get; set; } = 1;

    public List<TransformStageSlot> TransformStages { get; set; } = [];
}

public sealed class TransformStageSlot
{
    public string Kind { get; set; } = "";

    public string? ParametersJson { get; set; }
}
