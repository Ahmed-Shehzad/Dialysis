namespace Dialysis.SmartConnect.Attachments;

/// <summary>
/// Read-only context handed to an <see cref="IAttachmentHandler"/>. Carries the slot's properties JSON,
/// the default channel MIME type, and an <see cref="IAttachmentStore"/> reference so JS-style handlers
/// can persist directly via their <c>addAttachment</c> global.
/// </summary>
public sealed class AttachmentHandlerContext
{
    public required Guid FlowId { get; init; }

    public required Guid MessageId { get; init; }

    public string ChannelMimeType { get; init; } = "application/octet-stream";

    /// <summary>Handler-specific JSON from the pipeline slot. Empty/null means defaults.</summary>
    public string? PropertiesJson { get; init; }

    public required IAttachmentStore Store { get; init; }
}
