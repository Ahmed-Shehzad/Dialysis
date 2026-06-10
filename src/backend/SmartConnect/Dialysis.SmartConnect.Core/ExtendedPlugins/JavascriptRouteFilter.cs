using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.CodeTemplates;
using Dialysis.SmartConnect.Scripts;
using Dialysis.SmartConnect.VariableMaps;
using Jint;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>
/// Sandboxed JavaScript route filter via Jint. Parameters JSON must include <c>script</c>.
/// Exposes <c>payloadText</c>, <c>metadata</c> (object), <c>correlationId</c>, <c>flowId</c>, plus the
/// full Mirth-style variable map binding (sourceMap, channelMap, connectorMap, responseMap,
/// globalChannelMap, globalMap, configurationMap) and the <c>$(key)</c> walker.
/// Truthy return → Allow; falsy → Drop.
/// </summary>
public sealed class JavascriptRouteFilter : IRouteFilter
{
    private readonly IServiceProvider? _services;
    /// <summary>
    /// Sandboxed JavaScript route filter via Jint. Parameters JSON must include <c>script</c>.
    /// Exposes <c>payloadText</c>, <c>metadata</c> (object), <c>correlationId</c>, <c>flowId</c>, plus the
    /// full Mirth-style variable map binding (sourceMap, channelMap, connectorMap, responseMap,
    /// globalChannelMap, globalMap, configurationMap) and the <c>$(key)</c> walker.
    /// Truthy return → Allow; falsy → Drop.
    /// </summary>
    public JavascriptRouteFilter(IServiceProvider? services = null) => _services = services;
    public const string ParametersMetadataKey = "smartconnect.filter.parameters";

    public string Kind => "javascript";

    public async Task<RouteFilterResult> EvaluateAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        if (!message.Metadata.TryGetValue(ParametersMetadataKey, out var json) || string.IsNullOrWhiteSpace(json))
        {
            return RouteFilterResult.Allow();
        }

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("script", out var scriptEl))
        {
            return RouteFilterResult.Allow();
        }

        var script = scriptEl.GetString();
        if (string.IsNullOrWhiteSpace(script))
        {
            return RouteFilterResult.Allow();
        }

        var payloadText = message.PayloadFormat is PayloadFormat.Utf8Text or PayloadFormat.PlainText or PayloadFormat.Json
            ? Encoding.UTF8.GetString(message.Payload.Span)
            : Convert.ToBase64String(message.Payload.Span);

        cancellationToken.ThrowIfCancellationRequested();

        using var engine = new Engine(options =>
        {
            options.LimitRecursion(64);
            options.TimeoutInterval(TimeSpan.FromSeconds(3));
        });

        engine.SetValue("payloadText", payloadText);
        engine.SetValue("correlationId", message.CorrelationId);
        engine.SetValue("flowId", message.FlowId.ToString());

        var metaDict = new Dictionary<string, string>(message.Metadata.Count);
        foreach (var kvp in message.Metadata)
        {
            metaDict[kvp.Key] = kvp.Value;
        }

        engine.SetValue("metadata", metaDict);

        await BindVariableMapsAsync(engine, message, cancellationToken).ConfigureAwait(false);
        await BindCodeTemplatesAsync(engine, message.FlowId, cancellationToken).ConfigureAwait(false);
        BindAddAttachment(engine, message, cancellationToken);

        var result = (await engine.EvaluateAsync(script!, cancellationToken: cancellationToken).ConfigureAwait(false)).ToObject();
        var truthy = result switch
        {
            bool b => b,
            int i => i != 0,
            long l => l != 0,
            // JS truthiness is an exact-zero test by definition (0 and NaN are falsy),
            // so an epsilon range would be wrong here.
#pragma warning disable S1244
            double d => d < 0 || d > 0,
#pragma warning restore S1244
            string s => !string.IsNullOrEmpty(s),
            null => false,
            _ => true,
        };

        return truthy ? RouteFilterResult.Allow() : RouteFilterResult.Drop();
    }

    private async Task BindVariableMapsAsync(Engine engine, IntegrationMessage message, CancellationToken ct)
    {
        if (_services is null) return;

        var accessor = _services.GetService<IFlowExecutionContextAccessor>();
        var ctx = accessor?.Current ?? new FlowExecutionContext();

        var store = _services.GetService<IVariableMapStore>();
        IReadOnlyDictionary<string, string> globalChannel = new Dictionary<string, string>();
        IReadOnlyDictionary<string, string> global = new Dictionary<string, string>();
        IReadOnlyDictionary<string, string> configuration = new Dictionary<string, string>();
        if (store is not null)
        {
            globalChannel = await store.GetAllAsync(VariableMapScope.GlobalChannel, message.FlowId, ct).ConfigureAwait(false);
            global = await store.GetAllAsync(VariableMapScope.Global, null, ct).ConfigureAwait(false);
            configuration = await store.GetAllAsync(VariableMapScope.Configuration, null, ct).ConfigureAwait(false);
        }

        VariableMapsJsBinder.BindAll(engine, ctx, globalChannel, global, configuration);
    }

    private async Task BindCodeTemplatesAsync(Engine engine, Guid flowId, CancellationToken ct)
    {
        if (_services is null) return;
        var repo = _services.GetService<ICodeTemplateLibraryRepository>();
        if (repo is null) return;
        var accessor = _services.GetService<IFlowExecutionContextAccessor>();
        // RouteFilters in SmartConnect's model are pipeline-level pre-route gates → map to SourceFilter.
        // If a route-scoped filter context is set on the accessor (e.g. DestinationFilter), prefer it.
        var current = accessor?.Current?.CurrentStageContext;
        var context = current == CodeTemplateContext.DestinationFilter
            ? CodeTemplateContext.DestinationFilter
            : CodeTemplateContext.SourceFilter;
        await CodeTemplateJsBinder.PrependLinkedTemplatesAsync(engine, repo, flowId, context, ct).ConfigureAwait(false);
    }

    private void BindAddAttachment(Engine engine, IntegrationMessage message, CancellationToken ct)
    {
        var store = _services?.GetService<IAttachmentStore>();
        AttachmentJsBinder.Bind(engine, store, message.FlowId, message.Id, "application/octet-stream", ct);
    }
}
