namespace Dialysis.SmartConnect.Attachments;

/// <summary>
/// Pipeline configuration for the channel's attachment handler (Mirth UG p219).
/// <c>Kind</c> maps to a registered <see cref="IAttachmentHandler"/>; <c>PropertiesJson</c> is handler-specific.
/// </summary>
public sealed class AttachmentHandlerSlot
{
    public string Kind { get; set; } = "none";

    public string? PropertiesJson { get; set; }

    public string MimeType { get; set; } = "application/octet-stream";
}
