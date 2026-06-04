using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.SmartConnect.Contracts.Integration;

/// <summary>
/// SmartConnect persisted an attachment (form scan, DICOM study, HL7 message blob) and assigned it
/// an id. The HIE module's XDS Registry consumes this to publish the document into the cross-
/// organization document-sharing fabric (ITI-41 Provide and Register Document Set-b).
/// </summary>
/// <remarks>
/// The bytes themselves are NOT in the event — only the id, MIME type, size, and the originating
/// patient/source context. Consumers fetch the bytes via <c>IAttachmentBlobStore.ReadAsync</c> or
/// <c>IAttachmentDownloadUrlFactory.CreateAsync</c> if they need the content.
/// </remarks>
public sealed record AttachmentRegisteredIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// SmartConnect persisted an attachment (form scan, DICOM study, HL7 message blob) and assigned it
    /// an id. The HIE module's XDS Registry consumes this to publish the document into the cross-
    /// organization document-sharing fabric (ITI-41 Provide and Register Document Set-b).
    /// </summary>
    /// <remarks>
    /// The bytes themselves are NOT in the event — only the id, MIME type, size, and the originating
    /// patient/source context. Consumers fetch the bytes via <c>IAttachmentBlobStore.ReadAsync</c> or
    /// <c>IAttachmentDownloadUrlFactory.CreateAsync</c> if they need the content.
    /// </remarks>
    public AttachmentRegisteredIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid AttachmentId,
        Guid? MessageId,
        Guid? FlowId,
        string MimeType,
        long SizeBytes,
        string? PatientId,
        string? SourceOrgId)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.AttachmentId = AttachmentId;
        this.MessageId = MessageId;
        this.FlowId = FlowId;
        this.MimeType = MimeType;
        this.SizeBytes = SizeBytes;
        this.PatientId = PatientId;
        this.SourceOrgId = SourceOrgId;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid AttachmentId { get; init; }
    public Guid? MessageId { get; init; }
    public Guid? FlowId { get; init; }
    public string MimeType { get; init; }
    public long SizeBytes { get; init; }
    public string? PatientId { get; init; }
    public string? SourceOrgId { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid AttachmentId, out Guid? MessageId, out Guid? FlowId, out string MimeType, out long SizeBytes, out string? PatientId, out string? SourceOrgId)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        AttachmentId = this.AttachmentId;
        MessageId = this.MessageId;
        FlowId = this.FlowId;
        MimeType = this.MimeType;
        SizeBytes = this.SizeBytes;
        PatientId = this.PatientId;
        SourceOrgId = this.SourceOrgId;
    }
}
