using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIE.Consent.Domain;
using Dialysis.HIE.Consent.Ports;

namespace Dialysis.HIE.Consent.Features.GrantConsent;

public sealed class GrantConsentCommandHandler : ICommandHandler<GrantConsentCommand, Guid>
{
    private readonly IConsentRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    public GrantConsentCommandHandler(IConsentRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }
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
        await _repository.AddAsync(consent, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return consent.Id;
    }
}
