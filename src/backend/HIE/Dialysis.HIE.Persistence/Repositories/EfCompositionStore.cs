using Dialysis.HIE.OpenEhr.Domain;
using Dialysis.HIE.OpenEhr.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIE.Persistence.Repositories;

public sealed class EfCompositionStore : ICompositionStore
{
    private readonly HieDbContext _db;
    public EfCompositionStore(HieDbContext db) => _db = db;
    public Task AddAsync(Composition composition, CancellationToken cancellationToken = default) =>
        _db.Compositions.AddAsync(composition, cancellationToken).AsTask();

    public async Task<int> NextVersionAsync(Guid patientId, string archetypeId, CancellationToken cancellationToken = default)
    {
        var max = await _db.Compositions
            .Where(c => c.PatientId == patientId && c.ArchetypeId == archetypeId)
            .Select(c => (int?)c.Version)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false);
        return (max ?? 0) + 1;
    }
}
