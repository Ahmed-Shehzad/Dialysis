using System.Text;
using System.Text.Json;
using Jint;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>
/// Sandboxed JavaScript via Jint; parameters JSON must include <c>script</c> returning a string (new UTF-8 payload).
/// Exposes <c>payloadText</c> for UTF-8/PlainText/Json payloads, or Base64 for binary.
/// </summary>
public sealed class JavascriptTransformStage : ITransformStage
{
    public const string ParametersMetadataKey = "smartconnect.transform.parameters";

    public string Kind => "javascript";

    public Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        if (!message.Metadata.TryGetValue(ParametersMetadataKey, out var json) || string.IsNullOrWhiteSpace(json))
        {
            return Task.FromResult(message);
        }

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("script", out var scriptEl))
        {
            return Task.FromResult(message);
        }

        var script = scriptEl.GetString();
        if (string.IsNullOrWhiteSpace(script))
        {
            return Task.FromResult(message);
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
        var result = engine.Evaluate(script).ToObject();
        var str = result?.ToString() ?? "";
        return Task.FromResult(message.CloneWithPayload(Encoding.UTF8.GetBytes(str), PayloadFormat.Utf8Text));
    }
}
