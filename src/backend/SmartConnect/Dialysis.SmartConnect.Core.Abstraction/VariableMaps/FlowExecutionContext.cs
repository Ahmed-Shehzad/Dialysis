using System.Collections.Concurrent;
using System.Collections.Immutable;
using Dialysis.SmartConnect.CodeTemplates;

namespace Dialysis.SmartConnect.VariableMaps;

/// <summary>
/// Per-dispatch mutable context carrying Mirth-style message-scoped variable maps:
/// Source (read-only), Channel (cross-route), per-route Connector bags, and Response (auto-populated by the engine).
/// Lifetime: a single <see cref="IFlowRuntime"/> dispatch. Discarded when the dispatch completes.
/// </summary>
public sealed class FlowExecutionContext
{
    /// <summary>Identifier of the message currently being dispatched. Used by the JS <c>addAttachment</c> global.</summary>
    public Guid MessageId { get; init; }

    /// <summary>Identifier of the flow currently being dispatched. Used by the JS <c>addAttachment</c> global.</summary>
    public Guid FlowId { get; init; }

    public IReadOnlyDictionary<string, object?> SourceMap { get; init; } =
        ImmutableDictionary<string, object?>.Empty;

    public ConcurrentDictionary<string, object?> ChannelMap { get; } = new(StringComparer.Ordinal);

    public IReadOnlyList<ConcurrentDictionary<string, object?>> ConnectorMaps { get; init; } = [];

    public ConcurrentDictionary<string, object?> ResponseMap { get; } = new(StringComparer.Ordinal);

    /// <summary>Index of the route currently executing. -1 outside the per-route loop.</summary>
    public int CurrentRouteOrdinal { get; internal set; } = -1;

    /// <summary>
    /// Script-execution stage tag used by the Code Template engine to decide which templates to inject.
    /// Set by the runtime engine before invoking each JS-bearing plugin.
    /// </summary>
    public CodeTemplateContext CurrentStageContext { get; internal set; } = CodeTemplateContext.SourceTransformer;

    /// <summary>
    /// The connector map bag for the route currently executing. Outside the route loop returns a
    /// throwaway empty bag (writes are dropped).
    /// </summary>
    public ConcurrentDictionary<string, object?> CurrentConnectorMap =>
        CurrentRouteOrdinal >= 0 && CurrentRouteOrdinal < ConnectorMaps.Count
            ? ConnectorMaps[CurrentRouteOrdinal]
            : new ConcurrentDictionary<string, object?>(StringComparer.Ordinal);

    public void SetCurrentRouteOrdinal(int ordinal) => CurrentRouteOrdinal = ordinal;

    public void SetCurrentStageContext(CodeTemplateContext context) => CurrentStageContext = context;
}
