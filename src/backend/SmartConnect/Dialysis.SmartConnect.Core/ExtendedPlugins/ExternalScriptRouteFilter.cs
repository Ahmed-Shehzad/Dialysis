using System.Text;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.CodeTemplates;
using Dialysis.SmartConnect.Scripts;
using Dialysis.SmartConnect.VariableMaps;
using Jint;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>
/// Route filter that loads its JavaScript body from an external URI (Mirth UG p279
/// "External Script Filter Rule"). Properties JSON:
/// <c>{"scriptUri":"file:///... | https://...","cacheTtlSeconds":60}</c>. Falls through to
/// <see cref="RouteFilterResult.Allow"/> when no <c>scriptUri</c> is set. Context bindings
/// (payloadText, metadata, correlationId, flowId, variable maps, $() walker, addAttachment)
/// match <see cref="JavascriptRouteFilter"/> so external scripts are drop-in equivalents
/// of inline JS scripts.
/// </summary>
public sealed class ExternalScriptRouteFilter(IExternalScriptLoader scriptLoader, IServiceProvider? services = null) : IRouteFilter
{
    public const string KindValue = "external-script";
    public const string ParametersMetadataKey = JavascriptRouteFilter.ParametersMetadataKey;

    public string Kind => KindValue;

    public async Task<RouteFilterResult> EvaluateAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        if (!message.Metadata.TryGetValue(ParametersMetadataKey, out var json) || string.IsNullOrWhiteSpace(json))
        {
            return RouteFilterResult.Allow();
        }

        var (uri, ttl) = ExternalScriptParameters.Parse(json);
        if (uri is null)
        {
            return RouteFilterResult.Allow();
        }

        var script = await scriptLoader.LoadAsync(uri, ttl, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(script))
        {
            return RouteFilterResult.Allow();
        }

        var payloadText = message.PayloadFormat is PayloadFormat.Utf8Text or PayloadFormat.PlainText or PayloadFormat.Json
            ? Encoding.UTF8.GetString(message.Payload.Span)
            : Convert.ToBase64String(message.Payload.Span);

        cancellationToken.ThrowIfCancellationRequested();

        var engine = new Engine(options =>
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

        var result = engine.Evaluate(script).ToObject();
        var truthy = result switch
        {
            bool b => b,
            int i => i != 0,
            long l => l != 0,
            double d => d != 0,
            string s => !string.IsNullOrEmpty(s),
            null => false,
            _ => true,
        };

        return truthy ? RouteFilterResult.Allow() : RouteFilterResult.Drop();
    }

    private async Task BindVariableMapsAsync(Engine engine, IntegrationMessage message, CancellationToken ct)
    {
        if (services is null) return;
        var accessor = services.GetService<IFlowExecutionContextAccessor>();
        var ctx = accessor?.Current ?? new FlowExecutionContext();

        var store = services.GetService<IVariableMapStore>();
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
        if (services is null) return;
        var repo = services.GetService<ICodeTemplateLibraryRepository>();
        if (repo is null) return;
        var accessor = services.GetService<IFlowExecutionContextAccessor>();
        var current = accessor?.Current?.CurrentStageContext;
        var context = current == CodeTemplateContext.DestinationFilter
            ? CodeTemplateContext.DestinationFilter
            : CodeTemplateContext.SourceFilter;
        await CodeTemplateJsBinder.PrependLinkedTemplatesAsync(engine, repo, flowId, context, ct).ConfigureAwait(false);
    }

    private void BindAddAttachment(Engine engine, IntegrationMessage message, CancellationToken ct)
    {
        var store = services?.GetService<IAttachmentStore>();
        AttachmentJsBinder.Bind(engine, store, message.FlowId, message.Id, "application/octet-stream", ct);
    }
}
