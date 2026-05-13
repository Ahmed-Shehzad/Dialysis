namespace Dialysis.SmartConnect.Attachments;

/// <summary>
/// Runs once per inbound message, before route filters. Extracts bulky content into separate attachments
/// and replaces it inline with <c>${ATTACH:&lt;id&gt;}</c> tokens. Returns the rewritten payload + the
/// attachments the runtime should persist.
/// </summary>
public interface IAttachmentHandler
{
    string Kind { get; }

    Task<AttachmentHandlerResult> ExtractAsync(
        IntegrationMessage message,
        AttachmentHandlerContext context,
        CancellationToken cancellationToken);
}
