using Dialysis.Hie.OpenEhr.Domain;
using Dialysis.Hie.OpenEhr.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.Hie.Persistence.Repositories;

public sealed class EfCompositionStore(HieDbContext db) : ICompositionStore
{
    public Task AddAsync(Composition composition, CancellationToken cancellationToken = default) =>
        db.Compositions.AddAsync(composition, cancellationToken).AsTask();

    public async Task<int> NextVersionAsync(Guid patientId, string archetypeId, CancellationToken cancellationToken = default)
    {
        var max = await db.Compositions
            .Where(c => c.PatientId == patientId && c.ArchetypeId == archetypeId)
            .Select(c => (int?)c.Version)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false);
        return (max ?? 0) + 1;
    }
}
