using System.Text;
using System.Text.Json;

namespace Dialysis.SmartConnect.Attachments.Handlers;

/// <summary>
/// Stores the entire inbound payload as a single attachment and replaces the message with one
/// <c>${ATTACH:&lt;id&gt;}</c> token. Mirth UG p221 "Entire Message Attachment Handler".
/// Properties JSON (optional): <c>{ "mimeType": "application/pdf" }</c> overrides the slot's default MIME.
/// </summary>
public sealed class EntireMessageAttachmentHandler : IAttachmentHandler
{
    public const string KindValue = "entire-message";

    public string Kind => KindValue;

    public Task<AttachmentHandlerResult> ExtractAsync(
        IntegrationMessage message,
        AttachmentHandlerContext context,
        CancellationToken cancellationToken)
    {
        var mime = ResolveMimeType(context);
        var attachmentId = Guid.CreateVersion7();
        var attachment = new Attachment
        {
            Id = attachmentId,
            MessageId = context.MessageId,
            FlowId = context.FlowId,
            MimeType = mime,
            Data = message.Payload,
            SizeBytes = message.Payload.Length,
            CreatedUtc = DateTimeOffset.UtcNow,
        };

        var token = AttachmentReference.Format(attachmentId);
        var rewritten = Encoding.UTF8.GetBytes(token);

        return Task.FromResult(new AttachmentHandlerResult
        {
            RewrittenPayload = rewritten,
            Attachments = [attachment],
            Extracted = true,
        });
    }

    private static string ResolveMimeType(AttachmentHandlerContext ctx)
    {
        if (!string.IsNullOrWhiteSpace(ctx.PropertiesJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(ctx.PropertiesJson);
                if (doc.RootElement.TryGetProperty("mimeType", out var el) && el.ValueKind == JsonValueKind.String)
                {
                    var m = el.GetString();
                    if (!string.IsNullOrWhiteSpace(m)) return m;
                }
            }
            catch (JsonException)
            {
                // fall through to channel default
            }
        }
        return ctx.ChannelMimeType;
    }
}
