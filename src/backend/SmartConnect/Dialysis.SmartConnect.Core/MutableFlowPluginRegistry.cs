using System.Collections.Concurrent;
using Dialysis.SmartConnect.Alerts;
using Dialysis.SmartConnect.Attachments;

namespace Dialysis.SmartConnect;

/// <summary>
/// In-process registry for route filters, transform stages, outbound adapters, attachment handlers,
/// and alert action providers keyed by <see cref="IRouteFilter.Kind"/> (etc.).
/// </summary>
public sealed class MutableFlowPluginRegistry : IFlowPluginRegistry
{
    private readonly ConcurrentDictionary<string, IRouteFilter> _routeFilters = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ITransformStage> _transformStages = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IOutboundAdapter> _outboundAdapters = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IAttachmentHandler> _attachmentHandlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IAlertActionProvider> _alertActionProviders = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterRouteFilter(IRouteFilter filter) =>
        _routeFilters[filter.Kind] = filter;

    public void RegisterTransformStage(ITransformStage stage) =>
        _transformStages[stage.Kind] = stage;

    public void RegisterOutboundAdapter(IOutboundAdapter adapter) =>
        _outboundAdapters[adapter.Kind] = adapter;

    public void RegisterAttachmentHandler(IAttachmentHandler handler) =>
        _attachmentHandlers[handler.Kind] = handler;

    public void RegisterAlertActionProvider(IAlertActionProvider provider) =>
        _alertActionProviders[provider.Kind] = provider;

    public IRouteFilter? TryResolveRouteFilter(string kind) =>
        _routeFilters.TryGetValue(kind, out var f) ? f : null;

    public ITransformStage? TryResolveTransformStage(string kind) =>
        _transformStages.TryGetValue(kind, out var t) ? t : null;

    public IOutboundAdapter? TryResolveOutboundAdapter(string kind) =>
        _outboundAdapters.TryGetValue(kind, out var o) ? o : null;

    public IAttachmentHandler? TryResolveAttachmentHandler(string kind) =>
        _attachmentHandlers.TryGetValue(kind, out var h) ? h : null;

    public IAlertActionProvider? TryResolveAlertActionProvider(string kind) =>
        _alertActionProviders.TryGetValue(kind, out var p) ? p : null;
}
