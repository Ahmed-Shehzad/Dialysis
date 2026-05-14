using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Hie.Consent.Domain;
using Dialysis.Hie.Consent.Ports;

namespace Dialysis.Hie.Consent.Features.GrantConsent;

public sealed class GrantConsentCommandHandler(IConsentRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<GrantConsentCommand, Guid>
{
    public async Task<Guid> HandleAsync(GrantConsentCommand request, CancellationToken cancellationToken)
    {
        if (request.EffectiveToUtc is { } end && end <= request.EffectiveFromUtc)
            throw new InvalidOperationException("EffectiveToUtc must be after EffectiveFromUtc.");

        var consent = new ConsentRecord(
            request.PatientId,
            request.PartnerId,
            request.Scope,
            request.Direction,
            request.EffectiveFromUtc,
            request.EffectiveToUtc);
        await repository.AddAsync(consent, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return consent.Id;
    }
}
