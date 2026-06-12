using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Registration.Domain;
using Dialysis.EHR.Registration.Ports;

namespace Dialysis.EHR.Registration.Features.RegisterPatient;

public sealed class RegisterPatientCommandHandler : ICommandHandler<RegisterPatientCommand, Guid>
{
    private readonly IPatientRepository _patients;
    private readonly IUnitOfWork _unitOfWork;
    public RegisterPatientCommandHandler(IPatientRepository patients,
        IUnitOfWork unitOfWork)
    {
        _patients = patients;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(RegisterPatientCommand request, CancellationToken cancellationToken)
    {
        if (await _patients.FindByMedicalRecordNumberAsync(request.MedicalRecordNumber, cancellationToken).ConfigureAwait(false) is not null)
            throw new DomainException($"MRN '{request.MedicalRecordNumber}' is already in use.");

        var id = Guid.CreateVersion7();
        var name = new HumanName(request.FamilyName, request.GivenName, request.MiddleName);
        var patient = Patient.Register(
            id,
            request.MedicalRecordNumber,
            name,
            request.DateOfBirth,
            request.SexAtBirthCode,
            request.PreferredLanguageCode);

        _patients.Add(patient);


        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
