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
/// <see cref="IModuleDataExtractor"/>).
/// </summary>
public interface IDataSubjectRightsService
{
    /// <summary>Art. 15 + Art. 20 — export every aggregate the modules expose for the patient.</summary>
    Task<DataSubjectExport> ExportAsync(Guid patientId, CancellationToken cancellationToken);

    /// <summary>Art. 17 — file an erasure request. Returns the new request id; an operator
    /// reviews against legal-hold and either approves or rejects.</summary>
    Task<Guid> RequestErasureAsync(
        Guid patientId, string requestedBy, string? reason, CancellationToken cancellationToken);

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
