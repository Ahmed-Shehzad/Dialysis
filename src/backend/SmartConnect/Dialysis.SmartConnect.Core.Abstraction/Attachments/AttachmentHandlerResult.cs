namespace Dialysis.SmartConnect.Attachments;

/// <summary>
/// Outcome of <see cref="IAttachmentHandler.ExtractAsync"/>.
/// </summary>
public sealed class AttachmentHandlerResult
{
    public required ReadOnlyMemory<byte> RewrittenPayload { get; init; }

    /// <summary>Attachments the runtime should persist after the handler returns.</summary>
    public IReadOnlyList<Attachment> Attachments { get; init; } = [];

    /// <summary>
    /// True when the handler modified the payload or produced attachments. False means the runtime
    /// should keep the original message unchanged (used by the <c>none</c> handler).
    /// </summary>
    public bool Extracted { get; init; }

    public static AttachmentHandlerResult Unchanged(ReadOnlyMemory<byte> payload) => new()
    {
        RewrittenPayload = payload,
        Attachments = [],
        Extracted = false,
    };
}
