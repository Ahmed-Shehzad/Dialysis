using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Contracts.PatientLifecycle;
using Dialysis.HIS.PatientFlow.Domain;
using Dialysis.HIS.PatientFlow.Ports;

namespace Dialysis.HIS.PatientFlow.Features.RegisterPatient;

public sealed class RegisterPatientCommandHandler(
    IPatientRepository patients,
    IUnitOfWork unitOfWork,
    IEnumerable<IPatientRegisteredLifecycleHook> patientRegisteredHooks)
    : ICommandHandler<RegisterPatientCommand, Guid>
{
    public async Task<Guid> Handle(RegisterPatientCommand request, CancellationToken cancellationToken)
    {
        if (await patients.FindByMedicalRecordNumberAsync(request.MedicalRecordNumber, cancellationToken).ConfigureAwait(false) is not null)
            throw new InvalidOperationException($"MRN '{request.MedicalRecordNumber}' is already in use.");

        var id = Guid.CreateVersion7();
        var patient = new Patient(id);
        patient.RegisterNewPatient(request.MedicalRecordNumber, DateTime.UtcNow, actorId: null);
        patients.Add(patient);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        foreach (var hook in patientRegisteredHooks)
            await hook.AfterPatientRegisteredAsync(id, cancellationToken).ConfigureAwait(false);
        return id;
    }
}
