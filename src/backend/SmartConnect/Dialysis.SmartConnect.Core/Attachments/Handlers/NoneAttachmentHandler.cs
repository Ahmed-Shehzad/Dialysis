using Dialysis.SmartConnect.Attachments;

namespace Dialysis.SmartConnect.Attachments.Handlers;

/// <summary>
/// Null handler: returns the payload unchanged. Selected by configuring no <c>AttachmentHandler</c>
/// slot on the pipeline, or kind = "none" explicitly. Mirth UG p219 "None".
/// </summary>
public sealed class NoneAttachmentHandler : IAttachmentHandler
{
    public const string KindValue = "none";

    public string Kind => KindValue;

    public Task<AttachmentHandlerResult> ExtractAsync(
        IntegrationMessage message,
        AttachmentHandlerContext context,
        CancellationToken cancellationToken) =>
        Task.FromResult(AttachmentHandlerResult.Unchanged(message.Payload));
}
