namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;

public sealed class AttachmentEntity
{
    public Guid Id { get; set; }

    public Guid MessageId { get; set; }

    public Guid FlowId { get; set; }

    public string MimeType { get; set; } = "application/octet-stream";

    /// <summary>
    /// Bytes populated when the registered <see cref="Dialysis.SmartConnect.Attachments.IAttachmentBlobStore"/>
    /// reports <c>StoresBytesInRow = true</c> (the default <c>InRowAttachmentBlobStore</c>). Out-of-row
    /// implementations (filesystem, S3, Azure Blob) leave this null and store the bytes externally keyed
    /// by <see cref="Id"/>.
    /// </summary>
    public byte[]? Data { get; set; }

    public long SizeBytes { get; set; }

    public DateTimeOffset CreatedUtc { get; set; }
}
