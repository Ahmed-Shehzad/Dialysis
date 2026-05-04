using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.PatientFlow.Domain;
using Dialysis.HIS.PatientFlow.Integration;
using Dialysis.HIS.PatientFlow.Ports;

namespace Dialysis.HIS.PatientFlow.Features.CreateReferral;

public sealed class CreateReferralCommandHandler(
    IPatientRepository patients,
    IReferralRepository referrals,
    IUnitOfWork unitOfWork,
    ITransponderOutbox outbox)
    : ICommandHandler<CreateReferralCommand, Guid>
{
    public async Task<Guid> Handle(CreateReferralCommand request, CancellationToken cancellationToken)
    {
        _ = await patients.GetAsync(request.PatientId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Patient not found.");

        var id = Guid.CreateVersion7();
        var referral = Referral.Create(id, request.PatientId, request.ReferralTypeCode, DateTime.UtcNow, actorId: null);
        referrals.Add(referral);
        await OutboxFlush.ForAggregateAsync(referral, outbox, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
