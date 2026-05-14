using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIE.Consent.Ports;

namespace Dialysis.HIE.Consent.Features.RevokeConsent;

public sealed class RevokeConsentCommandHandler(IConsentRepository repository, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : ICommandHandler<RevokeConsentCommand>
{
    public async Task<Unit> HandleAsync(RevokeConsentCommand request, CancellationToken cancellationToken)
    {
        var consent = await repository.GetAsync(request.ConsentId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Consent {request.ConsentId} not found.");
        consent.Revoke(timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
