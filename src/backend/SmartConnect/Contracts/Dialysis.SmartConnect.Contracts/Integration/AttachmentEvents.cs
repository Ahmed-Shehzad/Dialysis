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
public sealed record AttachmentRegisteredIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid AttachmentId,
    Guid? MessageId,
    Guid? FlowId,
    string MimeType,
    long SizeBytes,
    string? PatientId,
    string? SourceOrgId) : IIntegrationEvent;
