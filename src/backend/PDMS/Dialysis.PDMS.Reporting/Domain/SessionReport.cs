using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.PDMS.Contracts.Integration;
using Dialysis.PDMS.Reporting.Contracts;

namespace Dialysis.PDMS.Reporting.Domain;

/// <summary>
/// A generated post-session document — discharge letter, shift report, or billing summary.
/// The byte content lives in an external blob store keyed by <see cref="StorageRef"/>; the
/// aggregate only carries the content hash so the audit gate can verify integrity without
/// pulling the body. State machine: <see cref="ReportStatus.Pending"/> →
/// <see cref="ReportStatus.Generated"/> → <see cref="ReportStatus.Delivered"/>? →
/// <see cref="ReportStatus.Archived"/>.
/// </summary>
public sealed class SessionReport : AggregateRoot<Guid>
{
    private SessionReport() { }

    public SessionReport(Guid id, Guid sessionId, Guid patientId, ReportKind kind) : base(id)
    {
        SessionId = sessionId;
        PatientId = patientId;
        Kind = kind;
        Status = ReportStatus.Pending;
    }

    public Guid SessionId { get; private set; }
    public Guid PatientId { get; private set; }
    public ReportKind Kind { get; private set; }
    public ReportStatus Status { get; private set; }
    public string Format { get; private set; } = "application/pdf";
    public string? ContentHash { get; private set; }
    public string? StorageRef { get; private set; }
    public DateTime? GeneratedAtUtc { get; private set; }
    public DateTime? DeliveredAtUtc { get; private set; }
    public string? FailureReason { get; private set; }

    /// <summary>
    /// Records a successful generation; emits the readiness integration event plus the
    /// cross-context <see cref="ClinicalDocumentProducedIntegrationEvent"/> (HIE Documents
    /// indexes the report as a FHIR DocumentReference over the same blob ref). Both are
    /// drained to the Transponder outbox atomically with the row on save.
    /// </summary>
    public void RecordGenerated(string storageRef, string contentHash, DateTime generatedAtUtc, string? languageCode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);
        StorageRef = storageRef;
        ContentHash = contentHash;
        GeneratedAtUtc = generatedAtUtc;
        Status = ReportStatus.Generated;
        RaiseIntegrationEvent(new SessionReportGeneratedIntegrationEvent
        {
            ReportId = Id,
            SessionId = SessionId,
            PatientId = PatientId,
            Kind = Kind.ToString(),
            StorageRef = storageRef,
            ContentHash = contentHash,
            GeneratedAtUtc = generatedAtUtc,
        });
        RaiseIntegrationEvent(new ClinicalDocumentProducedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            ReportId: Id,
            PatientId: PatientId,
            Kind: Kind.ToString(),
            MimeType: Format,
            Title: $"{Kind} — session {SessionId:N}",
            StorageRef: storageRef,
            ContentHash: contentHash,
            LanguageCode: languageCode));
    }

    /// <summary>Records a generation failure so the operator dashboard can show + retry.</summary>
    public void RecordFailure(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        FailureReason = reason;
        Status = ReportStatus.Failed;
    }

    /// <summary>Operator marked the document as delivered (handed to patient / sent to GP).</summary>
    public void MarkDelivered(DateTime deliveredAtUtc)
    {
        if (Status != ReportStatus.Generated)
            throw new InvalidOperationException($"Cannot deliver a report in {Status} state.");
        Status = ReportStatus.Delivered;
        DeliveredAtUtc = deliveredAtUtc;
    }

    /// <summary>Retention policy moves the report into archive after the configured window.</summary>
    public void Archive() => Status = ReportStatus.Archived;
}

public enum ReportStatus
{
    Pending = 0,
    Generated = 1,
    Delivered = 2,
    Archived = 3,
    Failed = 4,
}
