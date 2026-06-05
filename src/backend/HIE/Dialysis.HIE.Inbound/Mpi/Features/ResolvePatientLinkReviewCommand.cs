using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Inbound.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Inbound.Mpi.Features;

/// <summary>
/// The steward's adjudication of a suspected-duplicate pair: <see cref="Link"/> = same person,
/// otherwise the records are distinct. <see cref="ReviewedBy"/> is the authenticated steward.
/// </summary>
public sealed record ResolvePatientLinkReviewCommand(
    Guid ReviewId,
    bool Link,
    string? Note,
    string ReviewedBy) : ICommand, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.MpiStewardReview;
}

public sealed class ResolvePatientLinkReviewCommandHandler : ICommandHandler<ResolvePatientLinkReviewCommand, Unit>
{
    private readonly IPatientLinkReviewStore _store;
    private readonly TimeProvider _clock;
    public ResolvePatientLinkReviewCommandHandler(IPatientLinkReviewStore store, TimeProvider clock)
    {
        _store = store;
        _clock = clock;
    }

    public async Task<Unit> HandleAsync(ResolvePatientLinkReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _store.GetAsync(request.ReviewId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Patient link review not found.");

        review.Resolve(request.Link, request.ReviewedBy, request.Note, _clock.GetUtcNow().UtcDateTime);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
