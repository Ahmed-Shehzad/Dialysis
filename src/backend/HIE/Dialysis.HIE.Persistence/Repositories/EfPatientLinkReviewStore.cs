using Dialysis.HIE.Inbound.Mpi;
using Dialysis.HIE.Inbound.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIE.Persistence.Repositories;

public sealed class EfPatientLinkReviewStore : IPatientLinkReviewStore
{
    private readonly HieDbContext _db;
    public EfPatientLinkReviewStore(HieDbContext db) => _db = db;

    public void Add(PatientLinkReview review) => _db.PatientLinkReviews.Add(review);

    public Task<PatientLinkReview?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.PatientLinkReviews.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PatientLinkReview>> ListPendingAsync(int take, CancellationToken cancellationToken = default) =>
        await _db.PatientLinkReviews.AsNoTracking()
            .Where(r => r.Status == PatientLinkReviewStatus.Pending)
            .OrderByDescending(r => r.Score)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<bool> ExistsForPairAsync(Guid entryA, Guid entryB, CancellationToken cancellationToken = default) =>
        await _db.PatientLinkReviews.AsNoTracking().AnyAsync(
            r => (r.SourceEntryId == entryA && r.CandidateEntryId == entryB)
              || (r.SourceEntryId == entryB && r.CandidateEntryId == entryA),
            cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
