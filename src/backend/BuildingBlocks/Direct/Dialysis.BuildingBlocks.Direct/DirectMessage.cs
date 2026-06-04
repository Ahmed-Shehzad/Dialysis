namespace Dialysis.BuildingBlocks.Direct;

public sealed record DirectMessage
{
    public DirectMessage(string FromAddress,
        string ToAddress,
        string Subject,
        string TextBody,
        DirectAttachment? Attachment)
    {
        this.FromAddress = FromAddress;
        this.ToAddress = ToAddress;
        this.Subject = Subject;
        this.TextBody = TextBody;
        this.Attachment = Attachment;
    }
    public string FromAddress { get; init; }
    public string ToAddress { get; init; }
    public string Subject { get; init; }
    public string TextBody { get; init; }
    public DirectAttachment? Attachment { get; init; }
    public void Deconstruct(out string FromAddress, out string ToAddress, out string Subject, out string TextBody, out DirectAttachment? Attachment)
    {
        FromAddress = this.FromAddress;
        ToAddress = this.ToAddress;
        Subject = this.Subject;
        TextBody = this.TextBody;
        Attachment = this.Attachment;
    }
}

public sealed record DirectAttachment
{
    public DirectAttachment(string FileName, string ContentType, byte[] Payload)
    {
        this.FileName = FileName;
        this.ContentType = ContentType;
        this.Payload = Payload;
    }
    public string FileName { get; init; }
    public string ContentType { get; init; }
    public byte[] Payload { get; init; }
    public void Deconstruct(out string FileName, out string ContentType, out byte[] Payload)
    {
        FileName = this.FileName;
        ContentType = this.ContentType;
        Payload = this.Payload;
    }
}
