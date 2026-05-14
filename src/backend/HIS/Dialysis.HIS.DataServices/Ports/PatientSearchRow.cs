namespace Dialysis.HIS.DataServices.Ports;

/// <summary>
/// Projection of a patient indexed in the HIS full-text search corpus (RA Fig. 6 — Data management → Search).
/// HIS does not own patient master data — entries are pushed in by EHR (patient registration events) via the
/// <c>patients</c> corpus on <c>RaFullTextSearchEntries</c>. Until that wire is live, entries can be seeded
/// directly by integration tests or migrations.
/// </summary>
public sealed record PatientSearchRow(Guid Id, string ExternalPatientId, string SearchText, DateTime IndexedAtUtc);

public interface IPatientSearchReadModel
{
    Task<IReadOnlyList<PatientSearchRow>> SearchAsync(string? q, int skip, int take, CancellationToken cancellationToken = default);
}
