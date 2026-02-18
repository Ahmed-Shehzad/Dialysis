using Dialysis.Patient.Application.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Patient.Application.Features.RegisterPatient;

internal sealed class RegisterPatientCommandHandler : ICommandHandler<RegisterPatientCommand, RegisterPatientResponse>
{
    private readonly IPatientRepository _repository;

    public RegisterPatientCommandHandler(IPatientRepository repository)
    {
        _repository = repository;
    }

    public async Task<RegisterPatientResponse> HandleAsync(RegisterPatientCommand request, CancellationToken cancellationToken = default)
    {
        var patient = Domain.Patient.Register(
            request.MedicalRecordNumber,
            request.Name,
            request.DateOfBirth,
            request.Gender);

        _ = await _repository.AddAsync(patient, cancellationToken);
        return new RegisterPatientResponse(patient.Id.ToString());
    }
}
