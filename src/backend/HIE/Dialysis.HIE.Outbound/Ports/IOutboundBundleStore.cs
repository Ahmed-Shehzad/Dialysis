using Dialysis.HIE.Outbound.Domain;

namespace Dialysis.HIE.Outbound.Ports;

public interface IOutboundBundleStore
{
    Task AddAsync(OutboundBundle bundle, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboundBundle>> ClaimPendingAsync(int batchSize, DateTime asOfUtc, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
