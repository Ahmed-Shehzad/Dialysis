using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.HIE.Outbound.Ports;

namespace Dialysis.HIE.Outbound.Features.RetryOutboundBundle;

public sealed class RetryOutboundBundleCommandHandler : ICommandHandler<RetryOutboundBundleCommand, Unit>
{
    private readonly IOutboundBundleStore _store;
    private readonly TimeProvider _timeProvider;
    public RetryOutboundBundleCommandHandler(IOutboundBundleStore store,
        TimeProvider timeProvider)
    {
        _store = store;
        _timeProvider = timeProvider;
    }
    public async Task<Unit> HandleAsync(RetryOutboundBundleCommand request, CancellationToken cancellationToken)
    {
        var bundle = await _store.GetByIdAsync(request.BundleId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Outbound bundle '{request.BundleId}' not found.");

        bundle.MarkForRetry(_timeProvider.GetUtcNow().UtcDateTime);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
