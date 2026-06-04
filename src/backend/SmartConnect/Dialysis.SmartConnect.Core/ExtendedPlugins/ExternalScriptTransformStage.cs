using System.Text;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.CodeTemplates;
using Dialysis.SmartConnect.Scripts;
using Dialysis.SmartConnect.VariableMaps;
using Jint;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>
/// Transform stage that loads its JavaScript body from an external URI (Mirth UG p283
/// "External Script Transformer Step"). Properties JSON:
/// <c>{"scriptUri":"file:///... | https://...","cacheTtlSeconds":60}</c>. Script return value
/// is coerced to string and becomes the new UTF-8 payload. Context bindings match
/// <see cref="JavascriptTransformStage"/>.
/// </summary>
public sealed class ExternalScriptTransformStage : ITransformStage
{
    private readonly IExternalScriptLoader _scriptLoader;
    private readonly IServiceProvider? _services;
    /// <summary>
    /// Transform stage that loads its JavaScript body from an external URI (Mirth UG p283
    /// "External Script Transformer Step"). Properties JSON:
    /// <c>{"scriptUri":"file:///... | https://...","cacheTtlSeconds":60}</c>. Script return value
    /// is coerced to string and becomes the new UTF-8 payload. Context bindings match
    /// <see cref="JavascriptTransformStage"/>.
    /// </summary>
    public ExternalScriptTransformStage(IExternalScriptLoader scriptLoader, IServiceProvider? services = null)
    {
        _scriptLoader = scriptLoader;
        _services = services;
    }
    public const string KindValue = "external-script";
    public const string ParametersMetadataKey = JavascriptTransformStage.ParametersMetadataKey;

    public string Kind => KindValue;

    public async Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        if (!message.Metadata.TryGetValue(ParametersMetadataKey, out var json) || string.IsNullOrWhiteSpace(json))
        {
            return message;
        }

        var (uri, ttl) = ExternalScriptParameters.Parse(json);
        if (uri is null)
        {
            return message;
        }

        var script = await _scriptLoader.LoadAsync(uri, ttl, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(script))
        {
            return message;
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

        await BindVariableMapsAsync(engine, message, cancellationToken).ConfigureAwait(false);
        await BindCodeTemplatesAsync(engine, message.FlowId, cancellationToken).ConfigureAwait(false);
        BindAddAttachment(engine, message, cancellationToken);

        var result = engine.Evaluate(script).ToObject();
        var str = result?.ToString() ?? "";
        return message.CloneWithPayload(Encoding.UTF8.GetBytes(str), PayloadFormat.Utf8Text);
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
        var context = accessor?.Current?.CurrentStageContext ?? CodeTemplateContext.SourceTransformer;
        await CodeTemplateJsBinder.PrependLinkedTemplatesAsync(engine, repo, flowId, context, ct).ConfigureAwait(false);
    }

    private void BindAddAttachment(Engine engine, IntegrationMessage message, CancellationToken ct)
    {
        if (_services is null)
        {
            AttachmentJsBinder.Bind(engine, store: null, message.FlowId, message.Id, "application/octet-stream", ct);
            return;
        }
        var store = _services.GetService<IAttachmentStore>();
        AttachmentJsBinder.Bind(engine, store, message.FlowId, message.Id, "application/octet-stream", ct);
    }
}
