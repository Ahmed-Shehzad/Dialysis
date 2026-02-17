using Dialysis.Domain.Aggregates;
using Dialysis.Persistence.Abstractions;

namespace Dialysis.Persistence;

public sealed class ObservationRepository : IObservationRepository
{
    private readonly DialysisDbContext _db;

    public ObservationRepository(DialysisDbContext db) => _db = db;

    public async Task AddAsync(Observation observation, CancellationToken cancellationToken = default)
    {
        _db.Observations.Add(observation);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
