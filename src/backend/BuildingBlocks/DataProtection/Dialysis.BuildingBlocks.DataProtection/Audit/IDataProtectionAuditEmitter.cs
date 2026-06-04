namespace Dialysis.BuildingBlocks.DataProtection.Audit;

/// <summary>
/// Extends the existing HIPAA PHI-access audit with GDPR / BDSG / PDSG citations. Every
/// command guarded by <see cref="LawfulBasis.LawfulBasisAttribute"/> emits one row capturing:
/// who accessed what, why (lawful basis), under which §-citation, and the consent record id
/// (where applicable). PDSG ePA-document access carries an additional <see cref="EpaContext"/>.
///
/// Implementation persists via the existing FHIR Audit store (BuildingBlocks/Fhir.Audit), so
/// the DPO's existing audit-query UI surfaces these rows alongside HIPAA's PHI-access rows.
/// </summary>
public interface IDataProtectionAuditEmitter
{
    Task EmitAsync(DataProtectionAuditEvent evt, CancellationToken cancellationToken);
}

public sealed record DataProtectionAuditEvent
{
    public DataProtectionAuditEvent(DateTimeOffset OccurredAtUtc,
        string ModuleSlug,
        string Activity,
        string ActorSub,
        Guid? SubjectPatientId,
        string LawfulBasisCode,
        IReadOnlyList<string> Citations,
        Guid? ConsentRecordId,
        EpaContext? EpaContext,
        string? Detail)
    {
        this.OccurredAtUtc = OccurredAtUtc;
        this.ModuleSlug = ModuleSlug;
        this.Activity = Activity;
        this.ActorSub = ActorSub;
        this.SubjectPatientId = SubjectPatientId;
        this.LawfulBasisCode = LawfulBasisCode;
        this.Citations = Citations;
        this.ConsentRecordId = ConsentRecordId;
        this.EpaContext = EpaContext;
        this.Detail = Detail;
    }
    public DateTimeOffset OccurredAtUtc { get; init; }
    public string ModuleSlug { get; init; }
    public string Activity { get; init; }
    public string ActorSub { get; init; }
    public Guid? SubjectPatientId { get; init; }
    public string LawfulBasisCode { get; init; }
    public IReadOnlyList<string> Citations { get; init; }
    public Guid? ConsentRecordId { get; init; }
    public EpaContext? EpaContext { get; init; }
    public string? Detail { get; init; }
    public void Deconstruct(out DateTimeOffset OccurredAtUtc, out string ModuleSlug, out string Activity, out string ActorSub, out Guid? SubjectPatientId, out string LawfulBasisCode, out IReadOnlyList<string> Citations, out Guid? ConsentRecordId, out EpaContext? EpaContext, out string? Detail)
    {
        OccurredAtUtc = this.OccurredAtUtc;
        ModuleSlug = this.ModuleSlug;
        Activity = this.Activity;
        ActorSub = this.ActorSub;
        SubjectPatientId = this.SubjectPatientId;
        LawfulBasisCode = this.LawfulBasisCode;
        Citations = this.Citations;
        ConsentRecordId = this.ConsentRecordId;
        EpaContext = this.EpaContext;
        Detail = this.Detail;
    }
}

/// <summary>Optional PDSG-specific context: which ePA document, which sub-folder, etc.</summary>
public sealed record EpaContext
{
    /// <summary>Optional PDSG-specific context: which ePA document, which sub-folder, etc.</summary>
    public EpaContext(string DocumentId,
        string? FolderPath,
        string EpaEnvironment)
    {
        this.DocumentId = DocumentId;
        this.FolderPath = FolderPath;
        this.EpaEnvironment = EpaEnvironment;
    }
    public string DocumentId { get; init; }
    public string? FolderPath { get; init; }
    public string EpaEnvironment { get; init; }
    public void Deconstruct(out string DocumentId, out string? FolderPath, out string EpaEnvironment)
    {
        DocumentId = this.DocumentId;
        FolderPath = this.FolderPath;
        EpaEnvironment = this.EpaEnvironment;
    }
}
