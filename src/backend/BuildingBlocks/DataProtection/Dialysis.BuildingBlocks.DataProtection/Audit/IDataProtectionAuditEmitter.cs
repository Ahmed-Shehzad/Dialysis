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

public sealed record DataProtectionAuditEvent(
    DateTimeOffset OccurredAtUtc,
    string ModuleSlug,
    string Activity,
    string ActorSub,
    Guid? SubjectPatientId,
    string LawfulBasisCode,
    IReadOnlyList<string> Citations,
    Guid? ConsentRecordId,
    EpaContext? EpaContext,
    string? Detail);

/// <summary>Optional PDSG-specific context: which ePA document, which sub-folder, etc.</summary>
public sealed record EpaContext(
    string DocumentId,
    string? FolderPath,
    string EpaEnvironment);
