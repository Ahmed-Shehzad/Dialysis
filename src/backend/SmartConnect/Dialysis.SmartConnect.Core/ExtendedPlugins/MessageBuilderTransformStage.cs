using System.Text;
using System.Text.Json;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>
/// Message Builder-style transform: optional UTF-8 prefix/suffix around the payload (Mirth subset).
/// Parameters JSON: { "prefix": "...", "suffix": "..." }
/// </summary>
public sealed class MessageBuilderTransformStage : ITransformStage
{
    public const string KindValue = "message-builder";

    public string Kind => KindValue;

    public Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        if (!message.Metadata.TryGetValue("smartconnect.transform.parameters", out var raw) ||
            string.IsNullOrWhiteSpace(raw))
            return Task.FromResult(message);

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var prefix = root.TryGetProperty("prefix", out var p) ? p.GetString() ?? "" : "";
            var suffix = root.TryGetProperty("suffix", out var s) ? s.GetString() ?? "" : "";
            if (prefix.Length == 0 && suffix.Length == 0)
                return Task.FromResult(message);

            var body = Encoding.UTF8.GetString(message.Payload.Span);
            var combined = prefix + body + suffix;
            return Task.FromResult(message.CloneWithPayload(Encoding.UTF8.GetBytes(combined)));
        }
        catch (JsonException)
        {
            return Task.FromResult(message);
        }
    }
}
