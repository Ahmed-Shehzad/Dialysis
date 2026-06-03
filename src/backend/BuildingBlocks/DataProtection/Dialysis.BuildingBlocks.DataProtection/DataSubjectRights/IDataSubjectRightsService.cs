namespace Dialysis.BuildingBlocks.DataProtection.DataSubjectRights;

/// <summary>
/// GDPR Chapter III data-subject rights:
/// <list type="bullet">
///   <item>Art. 15 — Right of access (the subject can request a full copy of their data).</item>
///   <item>Art. 17 — Right to erasure ("right to be forgotten"). Legal-hold checks apply
///     (clinical records have a 30-year minimum retention under Berufsordnung §10).</item>
///   <item>Art. 18 — Right to restriction of processing.</item>
///   <item>Art. 20 — Right to data portability (machine-readable format).</item>
/// </list>
///
/// The platform's HTTP endpoints under <c>/api/v1.0/data-subject-rights/{patientId}/...</c>
/// dispatch into this service. Implementation per-deployment plugs into the actual data
/// stores (modules can register their own data exporter / eraser via
/// <see cref="IModuleDataExtractor"/> + <see cref="Erasure.IPatientEraser"/>).
/// </summary>
public interface IDataSubjectRightsService
{
    /// <summary>Art. 15 + Art. 20 — export every aggregate the modules expose for the patient.</summary>
    Task<DataSubjectExport> ExportAsync(Guid patientId, CancellationToken cancellationToken);

    /// <summary>Art. 17 — file an erasure request. Returns the new request id; an operator
    /// reviews against legal-hold and either approves or rejects.</summary>
    Task<Guid> RequestErasureAsync(
        Guid patientId, string requestedBy, string? reason, CancellationToken cancellationToken);

    /// <summary>
    /// Art. 17 — approve a pending request and run every registered
    /// <see cref="Erasure.IPatientEraser"/> in sequence. Returns the request after the
    /// composite outcome is persisted on it (per-module results + executed status).
    /// </summary>
    Task<ErasureRequest> ApproveErasureRequestAsync(
        Guid requestId, string approvedBy, CancellationToken cancellationToken);

    /// <summary>
    /// Art. 17 — reject a pending request (legal-hold applies, request is duplicate, etc.).
    /// The audit row captures who rejected it and why so a regulator can verify the decision.
    /// </summary>
    Task<ErasureRequest> RejectErasureRequestAsync(
        Guid requestId, string rejectedBy, string reason, CancellationToken cancellationToken);

    /// <summary>Operator-facing list of pending erasure requests awaiting approval.</summary>
    Task<IReadOnlyList<ErasureRequest>> ListPendingErasureRequestsAsync(
        int take, CancellationToken cancellationToken);

    /// <summary>Art. 18 — restrict processing pending resolution of a dispute.</summary>
    Task<Guid> RequestRestrictionAsync(
        Guid patientId, string requestedBy, string? reason, CancellationToken cancellationToken);
}

/// <summary>
/// Each module ships a `IModuleDataExtractor` implementation that knows how to dump a
/// patient's data into a FHIR Bundle subset. The aggregator stitches them together for the
/// Art. 15 / 20 export.
/// </summary>
public interface IModuleDataExtractor
{
    string ModuleSlug { get; }

    /// <summary>
    /// Returns a list of module-specific resources (in FHIR JSON form when possible) for the
    /// patient. The aggregator merges them into a `Bundle` for the operator UI / download.
    /// </summary>
    Task<IReadOnlyList<DataSubjectResource>> ExtractAsync(
        Guid patientId, CancellationToken cancellationToken);
}

public sealed record DataSubjectResource(
    string ResourceType,
    string Identifier,
    string Json);

public sealed record DataSubjectExport(
    Guid PatientId,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<DataSubjectResource> Resources);

/// <summary>Status of an erasure request through the approval / execution pipeline.</summary>
public enum ErasureRequestStatus
{
    /// <summary>Filed by the subject; awaiting operator review.</summary>
    Pending = 1,
    /// <summary>Operator rejected the request (legal-hold applies, duplicate, etc.).</summary>
    Rejected = 2,
    /// <summary>Operator approved and every <c>IPatientEraser</c> ran to completion.</summary>
    Executed = 3,
}

/// <summary>
/// Audit trail row for one GDPR Art. 17 erasure request. Persisted before approval so a
/// regulator can verify both the inbound subject claim and the operator decision.
/// </summary>
public sealed record ErasureRequest(
    Guid Id,
    Guid PatientId,
    ErasureRequestStatus Status,
    string RequestedBy,
    DateTimeOffset RequestedAtUtc,
    string? Reason,
    string? DecisionBy,
    DateTimeOffset? DecisionAtUtc,
    string? DecisionReason,
    IReadOnlyList<ErasureModuleResult> ExecutionLog);

/// <summary>Per-module audit entry written when the operator approved the request.</summary>
public sealed record ErasureModuleResult(
    string ModuleSlug,
    int RecordsErased,
    IReadOnlyDictionary<string, int> ByCategory);
