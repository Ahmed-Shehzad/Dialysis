using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Registration.Ports;

namespace Dialysis.EHR.Registration.Features.MergePatients;

public sealed class MergePatientsCommandHandler(
    IPatientRepository patients,
    IUnitOfWork unitOfWork)
    : ICommandHandler<MergePatientsCommand, Unit>
{
    public async Task<Unit> HandleAsync(MergePatientsCommand request, CancellationToken cancellationToken)
    {
        if (request.SurvivingPatientId == request.SupersededPatientId)
            throw new InvalidOperationException("Cannot merge a patient into itself.");

        var surviving = await patients.GetAsync(request.SurvivingPatientId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Surviving patient '{request.SurvivingPatientId}' not found.");

        var superseded = await patients.GetAsync(request.SupersededPatientId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Superseded patient '{request.SupersededPatientId}' not found.");

        superseded.MergeInto(surviving.Id, surviving.MedicalRecordNumber);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
