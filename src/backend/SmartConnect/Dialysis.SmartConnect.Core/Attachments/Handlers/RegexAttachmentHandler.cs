using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Dialysis.SmartConnect.Attachments.Handlers;

/// <summary>
/// Extracts capture group 1 of each regex match into a separate attachment and replaces it inline with a
/// reference token. Mirth UG p222 "Regex Attachment Handler". Properties JSON: <c>{ "pattern": "...",
/// "mimeType": "application/pdf", "regexOptions": "IgnoreCase|Multiline" }</c>. If the pattern has no
/// capture group, the entire match is extracted.
/// </summary>
public sealed class RegexAttachmentHandler : IAttachmentHandler
{
    public const string KindValue = "regex";

    public string Kind => KindValue;

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(3);

    public Task<AttachmentHandlerResult> ExtractAsync(
        IntegrationMessage message,
        AttachmentHandlerContext context,
        CancellationToken cancellationToken)
    {
        var (pattern, mime, options) = ParseProperties(context);
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return Task.FromResult(AttachmentHandlerResult.Unchanged(message.Payload));
        }

        var payloadText = Encoding.UTF8.GetString(message.Payload.Span);
        var regex = new Regex(pattern, options, RegexTimeout);
        var matches = regex.Matches(payloadText);
        if (matches.Count == 0)
        {
            return Task.FromResult(AttachmentHandlerResult.Unchanged(message.Payload));
        }

        var attachments = new List<Attachment>(matches.Count);
        var sb = new StringBuilder(payloadText.Length);
        var cursor = 0;

        foreach (Match m in matches)
        {
            var capture = m.Groups.Count > 1 && m.Groups[1].Success ? m.Groups[1] : (Capture)m;
            if (capture.Index < cursor) continue;

            sb.Append(payloadText, cursor, capture.Index - cursor);
            var id = Guid.CreateVersion7();
            var bytes = Encoding.UTF8.GetBytes(capture.Value);
            attachments.Add(new Attachment
            {
                Id = id,
                MessageId = context.MessageId,
                FlowId = context.FlowId,
                MimeType = mime,
                Data = bytes,
                SizeBytes = bytes.Length,
                CreatedUtc = DateTimeOffset.UtcNow,
            });
            sb.Append(AttachmentReference.Format(id));
            cursor = capture.Index + capture.Length;
        }
        sb.Append(payloadText, cursor, payloadText.Length - cursor);

        var rewritten = Encoding.UTF8.GetBytes(sb.ToString());
        return Task.FromResult(new AttachmentHandlerResult
        {
            RewrittenPayload = rewritten,
            Attachments = attachments,
            Extracted = true,
        });
    }

    private static (string Pattern, string MimeType, RegexOptions Options) ParseProperties(AttachmentHandlerContext ctx)
    {
        var mime = ctx.ChannelMimeType;
        var pattern = string.Empty;
        var options = RegexOptions.CultureInvariant | RegexOptions.Compiled;

        if (string.IsNullOrWhiteSpace(ctx.PropertiesJson)) return (pattern, mime, options);

        try
        {
            using var doc = JsonDocument.Parse(ctx.PropertiesJson);
            if (doc.RootElement.TryGetProperty("pattern", out var p) && p.ValueKind == JsonValueKind.String)
            {
                pattern = p.GetString() ?? string.Empty;
            }
            if (doc.RootElement.TryGetProperty("mimeType", out var m) && m.ValueKind == JsonValueKind.String)
            {
                var s = m.GetString();
                if (!string.IsNullOrWhiteSpace(s)) mime = s;
            }
            if (doc.RootElement.TryGetProperty("regexOptions", out var o) && o.ValueKind == JsonValueKind.String)
            {
                foreach (var name in (o.GetString() ?? string.Empty).Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (Enum.TryParse<RegexOptions>(name, ignoreCase: true, out var flag))
                    {
                        options |= flag;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // fall through with empty pattern → unchanged
        }
        return (pattern, mime, options);
    }
}
