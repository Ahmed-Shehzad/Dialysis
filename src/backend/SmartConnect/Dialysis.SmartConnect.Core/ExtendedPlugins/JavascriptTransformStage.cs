using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.CodeTemplates;
using Dialysis.SmartConnect.DataTypes;
using Dialysis.SmartConnect.Scripts;
using Dialysis.SmartConnect.VariableMaps;
using Jint;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>
/// Sandboxed JavaScript via Jint; parameters JSON must include <c>script</c> returning a string (new UTF-8 payload).
/// Exposes <c>payloadText</c> for UTF-8/PlainText/Json payloads, or Base64 for binary, plus the full Mirth-style
/// variable map binding (sourceMap, channelMap, connectorMap, responseMap, globalChannelMap, globalMap,
/// configurationMap) and the <c>$(key)</c> walker.
/// </summary>
public sealed class JavascriptTransformStage : ITransformStage
{
    private readonly IServiceProvider? _services;
    /// <summary>
    /// Sandboxed JavaScript via Jint; parameters JSON must include <c>script</c> returning a string (new UTF-8 payload).
    /// Exposes <c>payloadText</c> for UTF-8/PlainText/Json payloads, or Base64 for binary, plus the full Mirth-style
    /// variable map binding (sourceMap, channelMap, connectorMap, responseMap, globalChannelMap, globalMap,
    /// configurationMap) and the <c>$(key)</c> walker.
    /// </summary>
    public JavascriptTransformStage(IServiceProvider? services = null) => _services = services;
    public const string ParametersMetadataKey = "smartconnect.transform.parameters";

    public string Kind => "javascript";

    public async Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        if (!message.Metadata.TryGetValue(ParametersMetadataKey, out var json) || string.IsNullOrWhiteSpace(json))
        {
            return message;
        }

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("script", out var scriptEl))
        {
            return message;
        }

        var script = scriptEl.GetString();
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

        // Best-effort HL7 v2 parse: if the payload looks like an MSH-prefixed HL7 v2 message,
        // expose it as `msg` so scripts can write `msg.GetValue("PID.3.1")` directly. On a parse
        // failure we leave `msg` unset — non-HL7 transformers continue to work unchanged.
        TryBindHl7Message(engine, payloadText);

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

        var bound = VariableMapsJsBinder.BindAll(engine, ctx, globalChannel, global, configuration);

        // Note: write-back of globalChannel/global mutations happens in ChannelScriptExecutor, not here.
        // Per-stage transformer scripts touching globalMap.put/globalChannelMap.put do not persist.
        _ = bound;
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

    private static void TryBindHl7Message(Engine engine, string payloadText)
    {
        // Cheap pre-check before invoking the parser — skip non-HL7 payloads quickly so non-HL7
        // transformer flows don't pay the parse cost. MSH at the very start is the cheapest signal.
        if (string.IsNullOrEmpty(payloadText) || !payloadText.StartsWith("MSH", StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            var parsed = Hl7V2Message.Parse(payloadText);
            engine.SetValue("msg", parsed);
        }
        catch (FormatException)
        {
            // Looked like HL7 but didn't parse — leave `msg` unset and let the script decide.
        }
        catch (ArgumentException)
        {
            // Same — caller's payload is empty / whitespace.
        }
    }
}
