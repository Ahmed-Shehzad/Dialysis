using Dialysis.HIS.DataServices.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

/// <summary>
/// Patient search backed by the <c>RaFullTextSearchEntries</c> table filtered to the <c>patients</c> corpus.
/// HIS does not own patient master data — entries are expected to be pushed in by EHR via the full-text
/// indexing pipeline. The corpus boundary keeps the surface honest about HIS being a read-only facade.
/// </summary>
public sealed class EfPatientSearchReadModel(HisDbContext db) : IPatientSearchReadModel
{
    public const string PatientCorpusCode = "patients";

    public async Task<IReadOnlyList<PatientSearchRow>> SearchAsync(
        string? q,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);
        skip = Math.Max(0, skip);

        var query = db.RaFullTextSearchEntries.AsNoTracking()
            .Where(x => x.CorpusCode == PatientCorpusCode);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var needle = q.Trim();
            query = query.Where(x => x.SearchText.Contains(needle) || x.ExternalId.Contains(needle));
        }

        return await query
            .OrderByDescending(x => x.IndexedAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(x => new PatientSearchRow(x.Id, x.ExternalId, x.SearchText, x.IndexedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
