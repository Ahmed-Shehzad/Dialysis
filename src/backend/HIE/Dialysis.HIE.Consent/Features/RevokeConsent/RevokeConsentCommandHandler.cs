using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIE.Consent.Ports;

namespace Dialysis.HIE.Consent.Features.RevokeConsent;

public sealed class RevokeConsentCommandHandler : ICommandHandler<RevokeConsentCommand>
{
    private readonly IConsentRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public RevokeConsentCommandHandler(IConsentRepository repository, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    public async Task<Unit> HandleAsync(RevokeConsentCommand request, CancellationToken cancellationToken)
    {
        var consent = await _repository.GetAsync(request.ConsentId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Consent {request.ConsentId} not found.");
        consent.Revoke(_timeProvider.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
