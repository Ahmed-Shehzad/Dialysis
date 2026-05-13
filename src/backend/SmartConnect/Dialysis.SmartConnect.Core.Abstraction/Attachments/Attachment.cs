namespace Dialysis.SmartConnect.Attachments;

/// <summary>
/// Binary content extracted from a message by an <see cref="IAttachmentHandler"/> and referenced in the
/// rewritten payload as <c>${ATTACH:&lt;id&gt;}</c>. Reattachment inflates these tokens back to bytes on
/// opt-in outbound routes.
/// </summary>
public sealed class Attachment
{
    public required Guid Id { get; init; }

    public required Guid MessageId { get; init; }

    public required Guid FlowId { get; init; }

    public string MimeType { get; init; } = "application/octet-stream";

    public required ReadOnlyMemory<byte> Data { get; init; }

    public long SizeBytes { get; init; }

    public DateTimeOffset CreatedUtc { get; init; }
}
