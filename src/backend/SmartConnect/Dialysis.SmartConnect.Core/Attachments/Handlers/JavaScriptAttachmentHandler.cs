using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.CodeTemplates;
using Dialysis.SmartConnect.Scripts;
using Dialysis.SmartConnect.VariableMaps;
using Jint;
using Jint.Native;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Attachments.Handlers;

/// <summary>
/// User-defined JS handler with <c>addAttachment(data, type) -&gt; token</c> + a mutable <c>msg</c> variable
/// the script returns/sets to drive the rewritten payload. Mirth UG p224 "JavaScript Attachment Handler".
/// Properties JSON: <c>{ "script": "..." }</c>. Variable maps and code templates (with context
/// <c>AttachmentHandler</c>) are bound alongside.
/// </summary>
public sealed class JavaScriptAttachmentHandler : IAttachmentHandler
{
    private readonly IServiceProvider _services;
    /// <summary>
    /// User-defined JS handler with <c>addAttachment(data, type) -&gt; token</c> + a mutable <c>msg</c> variable
    /// the script returns/sets to drive the rewritten payload. Mirth UG p224 "JavaScript Attachment Handler".
    /// Properties JSON: <c>{ "script": "..." }</c>. Variable maps and code templates (with context
    /// <c>AttachmentHandler</c>) are bound alongside.
    /// </summary>
    public JavaScriptAttachmentHandler(IServiceProvider services) => _services = services;
    public const string KindValue = "javascript";

    public string Kind => KindValue;

    public async Task<AttachmentHandlerResult> ExtractAsync(
        IntegrationMessage message,
        AttachmentHandlerContext context,
        CancellationToken cancellationToken)
    {
        var script = ExtractScript(context.PropertiesJson);
        if (string.IsNullOrWhiteSpace(script))
        {
            return AttachmentHandlerResult.Unchanged(message.Payload);
        }

        var payloadText = Encoding.UTF8.GetString(message.Payload.Span);
        using var engine = new Engine(opts =>
        {
            opts.LimitRecursion(64);
            opts.TimeoutInterval(TimeSpan.FromSeconds(5));
        });

        engine.SetValue("msg", payloadText);
        engine.SetValue("flowId", context.FlowId.ToString());
        engine.SetValue("messageId", context.MessageId.ToString());

        await BindVariableMapsAsync(engine, message, cancellationToken).ConfigureAwait(false);
        await BindCodeTemplatesAsync(engine, message.FlowId, cancellationToken).ConfigureAwait(false);

        // addAttachment(data, type) — persists via the supplied IAttachmentStore and returns the inline token.
        engine.SetValue("addAttachment", new Func<JsValue, JsValue, string>((data, type) =>
        {
            var bytes = JsValueToBytes(data);
            var mime = type.IsUndefined() || type.IsNull() ? context.ChannelMimeType : type.AsString();
            var attachment = new Attachment
            {
                Id = Guid.CreateVersion7(),
                MessageId = context.MessageId,
                FlowId = context.FlowId,
                MimeType = string.IsNullOrWhiteSpace(mime) ? "application/octet-stream" : mime,
                Data = bytes,
                SizeBytes = bytes.LongLength,
                CreatedUtc = DateTimeOffset.UtcNow,
            };
            var stored = context.Store.AddAsync(attachment, cancellationToken).GetAwaiter().GetResult();
            return AttachmentReference.Format(stored.Id);
        }));

        // Evaluate and prefer the script's explicit return; else read back the (possibly reassigned) global "msg".
        var result = engine.Evaluate(script);
        string rewritten;
        if (!result.IsUndefined() && !result.IsNull())
        {
            rewritten = result.ToString();
        }
        else
        {
            rewritten = engine.GetValue("msg").AsString();
        }

        return new AttachmentHandlerResult
        {
            RewrittenPayload = Encoding.UTF8.GetBytes(rewritten),
            // Attachments already persisted via addAttachment; runtime should not re-persist.
            Attachments = [],
            Extracted = true,
        };
    }

    private async Task BindVariableMapsAsync(Engine engine, IntegrationMessage message, CancellationToken ct)
    {
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
        _ = VariableMapsJsBinder.BindAll(engine, ctx, globalChannel, global, configuration);
    }

    private async Task BindCodeTemplatesAsync(Engine engine, Guid flowId, CancellationToken ct)
    {
        var repo = _services.GetService<ICodeTemplateLibraryRepository>();
        if (repo is null) return;
        await CodeTemplateJsBinder.PrependLinkedTemplatesAsync(engine, repo, flowId, CodeTemplateContext.AttachmentHandler, ct).ConfigureAwait(false);
    }

    private static string? ExtractScript(string? propertiesJson)
    {
        if (string.IsNullOrWhiteSpace(propertiesJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(propertiesJson);
            if (doc.RootElement.TryGetProperty("script", out var el) && el.ValueKind == JsonValueKind.String)
            {
                return el.GetString();
            }
        }
        catch (JsonException) { }
        return null;
    }

    private static byte[] JsValueToBytes(JsValue value)
    {
        if (value.IsString())
        {
            return Encoding.UTF8.GetBytes(value.AsString());
        }
        if (value.IsArray())
        {
            var arr = value.AsArray();
            var len = (int)arr.Length;
            var bytes = new byte[len];
            for (var i = 0; i < len; i++)
            {
                var v = arr[(uint)i];
                bytes[i] = (byte)(v.IsNumber() ? (byte)v.AsNumber() : 0);
            }
            return bytes;
        }
        if (value.IsObject())
        {
            // Best-effort: stringify to JSON.
            return Encoding.UTF8.GetBytes(value.ToString());
        }
        throw new ArgumentException("addAttachment expects a string or byte-array data argument.");
    }
}
