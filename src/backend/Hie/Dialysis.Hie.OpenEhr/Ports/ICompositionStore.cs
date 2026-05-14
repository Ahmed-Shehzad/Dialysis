using Dialysis.Hie.OpenEhr.Domain;

namespace Dialysis.Hie.OpenEhr.Ports;

public interface ICompositionStore
{
    Task AddAsync(Composition composition, CancellationToken cancellationToken = default);

    Task<int> NextVersionAsync(Guid patientId, string archetypeId, CancellationToken cancellationToken = default);
}
