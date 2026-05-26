using Dialysis.SmartConnect.Attachments;

namespace Dialysis.SmartConnect;

/// <summary>
/// Serializable pipeline for an <see cref="IntegrationFlow"/> (filters, then parallel outbound routes).
/// </summary>
public sealed class IntegrationFlowPipelineDefinition
{
    public List<RouteFilterSlot> RouteFilters { get; set; } = [];

    /// <summary>
    /// Channel-level attachment handler invoked once per inbound message between the PreProcessor and the
    /// route-filter loop. When null, the runtime treats the message as "no extraction" (Mirth's <c>None</c>
    /// handler).
    /// </summary>
    public AttachmentHandlerSlot? AttachmentHandler { get; set; }

    /// <summary>
    /// Source-side transform stages run once after RouteFilters and before the outbound-route loop. Mirth-equivalent
    /// of source-connector transformer steps. Used most often for Destination Set Filter (which computes per-message
    /// outbound routing). Output metadata (including <c>smartconnect.destinationSet</c>) propagates into the route loop.
    /// </summary>
    public List<TransformStageSlot> SourceTransformStages { get; set; } = [];

    /// <summary>
    /// When false (default), all outbound routes are attempted (Mirth-style parallel destinations).
    /// When true, routes run in list order; the first failure or missing adapter stops later routes (destination chain).
    /// </summary>
    public bool OutboundRoutesSequential { get; set; }

    public List<OutboundRouteSlot> OutboundRoutes { get; set; } = [];

    /// <summary>Optional channel-level lifecycle scripts.</summary>
    public FlowScriptsDefinition? Scripts { get; set; }

    /// <summary>
    /// Code Template Library Ids linked to this flow. Templates from these libraries are injected
    /// (alongside libraries whose own <c>LinkedFlowIds</c> include this flow) into every JS plugin
    /// whose stage context matches the template's contexts. See <c>CodeTemplateLinkageService</c>.
    /// </summary>
    public List<Guid> LinkedLibraryIds { get; set; } = [];

    /// <summary>
    /// Inbound subscriptions for the content-based router. Each slot describes a class of inbound
    /// messages this flow wants to receive (by source-connector kind and/or message type pattern).
    /// When a source connector dispatches through <c>IMessageRouter</c>, every Started flow whose
    /// subscriptions match the candidate message receives a copy of the dispatch.
    ///
    /// Empty list ⇒ this flow is only reachable via direct <c>DefaultFlowId</c> binding (legacy
    /// behaviour preserved).
    /// </summary>
    public List<InboundSubscriptionSlot> InboundSubscriptions { get; set; } = [];
}

/// <summary>
/// Content-based subscription predicate evaluated by <c>IMessageRouter</c>. Each non-null field is
/// AND-ed; nulls mean "any". Use <c>*</c> for wildcard in <see cref="MessageTypePattern"/>.
/// </summary>
public sealed class InboundSubscriptionSlot
{
    /// <summary>Source-connector kind filter (e.g. <c>mllp</c>, <c>http</c>, <c>file-reader</c>, <c>sftp</c>). Null = any.</summary>
    public string? SourceKind { get; set; }

    /// <summary>Glob-style pattern matched against the message-type metadata (e.g. HL7 trigger <c>ORU^R*</c>). Null = any.</summary>
    public string? MessageTypePattern { get; set; }

    /// <summary>Optional sender-id exact match (e.g. HL7 MSH-3 sending application). Null = any.</summary>
    public string? SenderId { get; set; }
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

    /// <summary>
    /// When true, inline <c>${ATTACH:&lt;id&gt;}</c> tokens are inflated to their stored bytes immediately
    /// before this route's outbound <c>SendAsync</c>. Default false (matches Mirth UG p220 per-destination toggle).
    /// </summary>
    public bool ReattachAttachments { get; set; }
}

public sealed class TransformStageSlot
{
    public string Kind { get; set; } = "";

    public string? ParametersJson { get; set; }
}
