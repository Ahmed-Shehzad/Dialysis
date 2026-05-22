using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.HIE.Outbound.Ports;

namespace Dialysis.HIE.Outbound.Features.RetryOutboundBundle;

public sealed class RetryOutboundBundleCommandHandler(
    IOutboundBundleStore store,
    TimeProvider timeProvider)
    : ICommandHandler<RetryOutboundBundleCommand, Unit>
{
    public async Task<Unit> HandleAsync(RetryOutboundBundleCommand request, CancellationToken cancellationToken)
    {
        var bundle = await store.GetByIdAsync(request.BundleId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Outbound bundle '{request.BundleId}' not found.");

        bundle.MarkForRetry(timeProvider.GetUtcNow().UtcDateTime);
        await store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
