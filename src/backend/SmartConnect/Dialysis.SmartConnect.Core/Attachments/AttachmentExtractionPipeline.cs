using Microsoft.Extensions.Logging;

namespace Dialysis.SmartConnect.Attachments;

/// <summary>
/// Invoked from <c>FlowRuntimeEngine</c> immediately after the PreProcessor and before route filters.
/// Resolves the channel's <see cref="AttachmentHandlerSlot"/> kind via <see cref="IFlowPluginRegistry"/>,
/// runs the handler, persists any extracted attachments, and returns the rewritten payload bytes.
/// </summary>
public sealed class AttachmentExtractionPipeline(
    IFlowPluginRegistry registry,
    IAttachmentStore store,
    ILogger<AttachmentExtractionPipeline>? logger = null)
{
    public async Task<ReadOnlyMemory<byte>> RunAsync(
        IntegrationMessage message,
        AttachmentHandlerSlot? slot,
        CancellationToken cancellationToken)
    {
        if (slot is null || string.Equals(slot.Kind, "none", StringComparison.OrdinalIgnoreCase))
        {
            return message.Payload;
        }

        var handler = registry.TryResolveAttachmentHandler(slot.Kind);
        if (handler is null)
        {
            logger?.LogWarning(
                "Attachment handler kind '{Kind}' is not registered; leaving payload untouched (flow {FlowId} message {MessageId}).",
                slot.Kind, message.FlowId, message.Id);
            return message.Payload;
        }

        var context = new AttachmentHandlerContext
        {
            FlowId = message.FlowId,
            MessageId = message.Id,
            ChannelMimeType = string.IsNullOrWhiteSpace(slot.MimeType) ? "application/octet-stream" : slot.MimeType,
            PropertiesJson = slot.PropertiesJson,
            Store = store,
        };

        var result = await handler.ExtractAsync(message, context, cancellationToken).ConfigureAwait(false);
        if (!result.Extracted)
        {
            return message.Payload;
        }

        foreach (var attachment in result.Attachments)
        {
            await store.AddAsync(attachment, cancellationToken).ConfigureAwait(false);
        }

        return result.RewrittenPayload;
    }
}
