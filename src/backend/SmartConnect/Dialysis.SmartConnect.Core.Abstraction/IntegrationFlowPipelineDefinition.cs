namespace Dialysis.SmartConnect;

/// <summary>
/// Serializable pipeline for an <see cref="IntegrationFlow"/> (filters, then parallel outbound routes).
/// </summary>
public sealed class IntegrationFlowPipelineDefinition
{
    public List<RouteFilterSlot> RouteFilters { get; set; } = [];

    /// <summary>
    /// When false (default), all outbound routes are attempted (Mirth-style parallel destinations).
    /// When true, routes run in list order; the first failure or missing adapter stops later routes (destination chain).
    /// </summary>
    public bool OutboundRoutesSequential { get; set; }

    public List<OutboundRouteSlot> OutboundRoutes { get; set; } = [];

    /// <summary>Optional channel-level lifecycle scripts.</summary>
    public FlowScriptsDefinition? Scripts { get; set; }
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

    /// <summary>Optional transform stages applied to the outbound response payload.</summary>
    public List<TransformStageSlot> ResponseTransformStages { get; set; } = [];
}

public sealed class TransformStageSlot
{
    public string Kind { get; set; } = "";

    public string? ParametersJson { get; set; }
}
