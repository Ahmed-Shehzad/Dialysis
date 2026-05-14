using Dialysis.HIE.Inbound.Domain;

namespace Dialysis.HIE.Inbound.Ports;

public interface IReceivedResourceStore
{
    Task UpsertAsync(ReceivedResource resource, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
