using Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;

namespace Dialysis.Persistence;

public sealed class EpisodeOfCareRepository : IEpisodeOfCareRepository
{
    private readonly DialysisDbContext _db;

    public EpisodeOfCareRepository(DialysisDbContext db) => _db = db;

    public async Task AddAsync(EpisodeOfCare episode, CancellationToken cancellationToken = default)
    {
        _db.EpisodeOfCare.Add(episode);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(EpisodeOfCare episode, CancellationToken cancellationToken = default)
    {
        _db.EpisodeOfCare.Update(episode);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(EpisodeOfCare episode, CancellationToken cancellationToken = default)
    {
        _db.EpisodeOfCare.Remove(episode);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
