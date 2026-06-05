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

    /// <summary>
    /// Art. 18 — file a restriction-of-processing request. Persists an
    /// <see cref="RestrictionRequestStatus.Active"/> audit row and returns its id; an operator
    /// lifts it once the dispute that prompted it is resolved.
    /// </summary>
    Task<Guid> RequestRestrictionAsync(
        Guid patientId, string requestedBy, string? reason, CancellationToken cancellationToken);

    /// <summary>Operator-facing list of active restriction requests.</summary>
    Task<IReadOnlyList<RestrictionRequest>> ListActiveRestrictionsAsync(
        int take, CancellationToken cancellationToken);

    /// <summary>
    /// Art. 18 — lift an active restriction once its dispute is resolved. The audit row captures
    /// who lifted it and why so a regulator can verify the decision.
    /// </summary>
    Task<RestrictionRequest> LiftRestrictionAsync(
        Guid requestId, string liftedBy, string reason, CancellationToken cancellationToken);
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

public sealed record DataSubjectResource
{
    public DataSubjectResource(string ResourceType,
        string Identifier,
        string Json)
    {
        this.ResourceType = ResourceType;
        this.Identifier = Identifier;
        this.Json = Json;
    }
    public string ResourceType { get; init; }
    public string Identifier { get; init; }
    public string Json { get; init; }
    public void Deconstruct(out string ResourceType, out string Identifier, out string Json)
    {
        ResourceType = this.ResourceType;
        Identifier = this.Identifier;
        Json = this.Json;
    }
}

public sealed record DataSubjectExport
{
    public DataSubjectExport(Guid PatientId,
        DateTimeOffset GeneratedAtUtc,
        IReadOnlyList<DataSubjectResource> Resources)
    {
        this.PatientId = PatientId;
        this.GeneratedAtUtc = GeneratedAtUtc;
        this.Resources = Resources;
    }
    public Guid PatientId { get; init; }
    public DateTimeOffset GeneratedAtUtc { get; init; }
    public IReadOnlyList<DataSubjectResource> Resources { get; init; }
    public void Deconstruct(out Guid PatientId, out DateTimeOffset GeneratedAtUtc, out IReadOnlyList<DataSubjectResource> Resources)
    {
        PatientId = this.PatientId;
        GeneratedAtUtc = this.GeneratedAtUtc;
        Resources = this.Resources;
    }
}

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
public sealed record ErasureRequest
{
    /// <summary>
    /// Audit trail row for one GDPR Art. 17 erasure request. Persisted before approval so a
    /// regulator can verify both the inbound subject claim and the operator decision.
    /// </summary>
    public ErasureRequest(Guid Id,
        Guid PatientId,
        ErasureRequestStatus Status,
        string RequestedBy,
        DateTimeOffset RequestedAtUtc,
        string? Reason,
        string? DecisionBy,
        DateTimeOffset? DecisionAtUtc,
        string? DecisionReason,
        IReadOnlyList<ErasureModuleResult> ExecutionLog)
    {
        this.Id = Id;
        this.PatientId = PatientId;
        this.Status = Status;
        this.RequestedBy = RequestedBy;
        this.RequestedAtUtc = RequestedAtUtc;
        this.Reason = Reason;
        this.DecisionBy = DecisionBy;
        this.DecisionAtUtc = DecisionAtUtc;
        this.DecisionReason = DecisionReason;
        this.ExecutionLog = ExecutionLog;
    }
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public ErasureRequestStatus Status { get; init; }
    public string RequestedBy { get; init; }
    public DateTimeOffset RequestedAtUtc { get; init; }
    public string? Reason { get; init; }
    public string? DecisionBy { get; init; }
    public DateTimeOffset? DecisionAtUtc { get; init; }
    public string? DecisionReason { get; init; }
    public IReadOnlyList<ErasureModuleResult> ExecutionLog { get; init; }
    public void Deconstruct(out Guid Id, out Guid PatientId, out ErasureRequestStatus Status, out string RequestedBy, out DateTimeOffset RequestedAtUtc, out string? Reason, out string? DecisionBy, out DateTimeOffset? DecisionAtUtc, out string? DecisionReason, out IReadOnlyList<ErasureModuleResult> ExecutionLog)
    {
        Id = this.Id;
        PatientId = this.PatientId;
        Status = this.Status;
        RequestedBy = this.RequestedBy;
        RequestedAtUtc = this.RequestedAtUtc;
        Reason = this.Reason;
        DecisionBy = this.DecisionBy;
        DecisionAtUtc = this.DecisionAtUtc;
        DecisionReason = this.DecisionReason;
        ExecutionLog = this.ExecutionLog;
    }
}

/// <summary>Per-module audit entry written when the operator approved the request.</summary>
public sealed record ErasureModuleResult
{
    /// <summary>Per-module audit entry written when the operator approved the request.</summary>
    public ErasureModuleResult(string ModuleSlug,
        int RecordsErased,
        IReadOnlyDictionary<string, int> ByCategory)
    {
        this.ModuleSlug = ModuleSlug;
        this.RecordsErased = RecordsErased;
        this.ByCategory = ByCategory;
    }
    public string ModuleSlug { get; init; }
    public int RecordsErased { get; init; }
    public IReadOnlyDictionary<string, int> ByCategory { get; init; }
    public void Deconstruct(out string ModuleSlug, out int RecordsErased, out IReadOnlyDictionary<string, int> ByCategory)
    {
        ModuleSlug = this.ModuleSlug;
        RecordsErased = this.RecordsErased;
        ByCategory = this.ByCategory;
    }
}
