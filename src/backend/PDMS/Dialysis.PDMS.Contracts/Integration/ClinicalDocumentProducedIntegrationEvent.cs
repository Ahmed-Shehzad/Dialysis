using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.PDMS.Contracts.Integration;

/// <summary>
/// Published every time PDMS Reporting finalises a clinical-document render (discharge
/// letter, shift report, billing summary). HIE Documents consumes the event and creates a
/// FHIR <c>DocumentReference</c> row pointing at the same <see cref="StorageRef"/>, so the
/// admin Documents view, ePA upload, and partner exchange resolve through one shared blob
/// store. PDMS continues to own the source <c>SessionReport</c> aggregate; the event is
/// a one-way notification, not a transfer of ownership.
/// </summary>
public sealed record ClinicalDocumentProducedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Published every time PDMS Reporting finalises a clinical-document render (discharge
    /// letter, shift report, billing summary). HIE Documents consumes the event and creates a
    /// FHIR <c>DocumentReference</c> row pointing at the same <see cref="StorageRef"/>, so the
    /// admin Documents view, ePA upload, and partner exchange resolve through one shared blob
    /// store. PDMS continues to own the source <c>SessionReport</c> aggregate; the event is
    /// a one-way notification, not a transfer of ownership.
    /// </summary>
    public ClinicalDocumentProducedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid ReportId,
        Guid PatientId,
        string Kind,
        string MimeType,
        string Title,
        string StorageRef,
        string ContentHash,
        string? LanguageCode)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.ReportId = ReportId;
        this.PatientId = PatientId;
        this.Kind = Kind;
        this.MimeType = MimeType;
        this.Title = Title;
        this.StorageRef = StorageRef;
        this.ContentHash = ContentHash;
        this.LanguageCode = LanguageCode;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid ReportId { get; init; }
    public Guid PatientId { get; init; }
    public string Kind { get; init; }
    public string MimeType { get; init; }
    public string Title { get; init; }
    public string StorageRef { get; init; }
    public string ContentHash { get; init; }
    public string? LanguageCode { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid ReportId, out Guid PatientId, out string Kind, out string MimeType, out string Title, out string StorageRef, out string ContentHash, out string? LanguageCode)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        ReportId = this.ReportId;
        PatientId = this.PatientId;
        Kind = this.Kind;
        MimeType = this.MimeType;
        Title = this.Title;
        StorageRef = this.StorageRef;
        ContentHash = this.ContentHash;
        LanguageCode = this.LanguageCode;
    }
}
