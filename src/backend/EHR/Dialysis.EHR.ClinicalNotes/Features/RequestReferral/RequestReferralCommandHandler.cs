using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Ports;

namespace Dialysis.EHR.ClinicalNotes.Features.RequestReferral;

public sealed class RequestReferralCommandHandler : ICommandHandler<RequestReferralCommand, Guid>
{
    private readonly IReferralRepository _referrals;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public RequestReferralCommandHandler(IReferralRepository referrals,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _referrals = referrals;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    public async Task<Guid> HandleAsync(RequestReferralCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        var referral = Referral.Request(
            id,
            request.PatientId,
            request.DestinationPartnerId,
            request.ReferringProviderId,
            request.ReferralReason,
            _timeProvider.GetUtcNow().UtcDateTime);
        _referrals.Add(referral);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
