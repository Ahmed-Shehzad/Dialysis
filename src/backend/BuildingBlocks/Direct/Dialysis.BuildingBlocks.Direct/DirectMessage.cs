namespace Dialysis.BuildingBlocks.Direct;

public sealed record DirectMessage(
    string FromAddress,
    string ToAddress,
    string Subject,
    string TextBody,
    DirectAttachment? Attachment);

public sealed record DirectAttachment(string FileName, string ContentType, byte[] Payload);
