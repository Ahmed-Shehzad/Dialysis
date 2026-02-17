using Dialysis.FhirStore.Data;

namespace Dialysis.FhirStore;

public sealed class ObservationStore : IObservationStore
{
    private readonly FhirStoreDbContext _db;

    public ObservationStore(FhirStoreDbContext db)
    {
        _db = db;
    }

    public async Task<string> CreateAsync(ObservationEntity entity, CancellationToken cancellationToken = default)
    {
        _db.Observations.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
