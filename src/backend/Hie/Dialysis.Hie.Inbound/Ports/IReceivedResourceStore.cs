using Dialysis.Hie.Inbound.Domain;

namespace Dialysis.Hie.Inbound.Ports;

public interface IReceivedResourceStore
{
    Task UpsertAsync(ReceivedResource resource, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
