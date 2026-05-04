using System.Collections.Concurrent;

namespace Dialysis.SmartConnect;

/// <summary>
/// In-process registry for route filters, transform stages, and outbound adapters keyed by <see cref="IRouteFilter.Kind"/> (etc.).
/// </summary>
public sealed class MutableFlowPluginRegistry : IFlowPluginRegistry
{
    private readonly ConcurrentDictionary<string, IRouteFilter> _routeFilters = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ITransformStage> _transformStages = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IOutboundAdapter> _outboundAdapters = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterRouteFilter(IRouteFilter filter) =>
        _routeFilters[filter.Kind] = filter;

    public void RegisterTransformStage(ITransformStage stage) =>
        _transformStages[stage.Kind] = stage;

    public void RegisterOutboundAdapter(IOutboundAdapter adapter) =>
        _outboundAdapters[adapter.Kind] = adapter;

    public IRouteFilter? TryResolveRouteFilter(string kind) =>
        _routeFilters.TryGetValue(kind, out var f) ? f : null;

    public ITransformStage? TryResolveTransformStage(string kind) =>
        _transformStages.TryGetValue(kind, out var t) ? t : null;

    public IOutboundAdapter? TryResolveOutboundAdapter(string kind) =>
        _outboundAdapters.TryGetValue(kind, out var o) ? o : null;
}
