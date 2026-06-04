namespace Dialysis.HIS.DataServices.Ports;

/// <summary>
/// Projection of a patient indexed in the HIS full-text search corpus (RA Fig. 6 — Data management → Search).
/// HIS does not own patient master data — entries are pushed in by EHR (patient registration events) via the
/// <c>patients</c> corpus on <c>RaFullTextSearchEntries</c>. Until that wire is live, entries can be seeded
/// directly by integration tests or migrations.
/// </summary>
public sealed record PatientSearchRow
{
    /// <summary>
    /// Projection of a patient indexed in the HIS full-text search corpus (RA Fig. 6 — Data management → Search).
    /// HIS does not own patient master data — entries are pushed in by EHR (patient registration events) via the
    /// <c>patients</c> corpus on <c>RaFullTextSearchEntries</c>. Until that wire is live, entries can be seeded
    /// directly by integration tests or migrations.
    /// </summary>
    public PatientSearchRow(Guid Id, string ExternalPatientId, string SearchText, DateTime IndexedAtUtc)
    {
        this.Id = Id;
        this.ExternalPatientId = ExternalPatientId;
        this.SearchText = SearchText;
        this.IndexedAtUtc = IndexedAtUtc;
    }
    public Guid Id { get; init; }
    public string ExternalPatientId { get; init; }
    public string SearchText { get; init; }
    public DateTime IndexedAtUtc { get; init; }
    public void Deconstruct(out Guid Id, out string ExternalPatientId, out string SearchText, out DateTime IndexedAtUtc)
    {
        Id = this.Id;
        ExternalPatientId = this.ExternalPatientId;
        SearchText = this.SearchText;
        IndexedAtUtc = this.IndexedAtUtc;
    }
}

public interface IPatientSearchReadModel
{
    Task<IReadOnlyList<PatientSearchRow>> SearchAsync(string? q, int skip, int take, CancellationToken cancellationToken = default);
}
