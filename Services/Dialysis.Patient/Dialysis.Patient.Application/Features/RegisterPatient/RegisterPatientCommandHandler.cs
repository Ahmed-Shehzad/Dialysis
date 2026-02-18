using BuildingBlocks.Tenancy;

using Dialysis.Patient.Application.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Patient.Application.Features.RegisterPatient;

internal sealed class RegisterPatientCommandHandler : ICommandHandler<RegisterPatientCommand, RegisterPatientResponse>
{
    private readonly IPatientRepository _repository;
    private readonly ITenantContext _tenant;

    public RegisterPatientCommandHandler(IPatientRepository repository, ITenantContext tenant)
    {
        _repository = repository;
        _tenant = tenant;
    }

    public async Task<RegisterPatientResponse> HandleAsync(RegisterPatientCommand request, CancellationToken cancellationToken = default)
    {
        var patient = Domain.Patient.Register(
            request.MedicalRecordNumber,
            request.Name,
            request.DateOfBirth,
            request.Gender,
            _tenant.TenantId);

        await _repository.AddAsync(patient, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return new RegisterPatientResponse(patient.Id.ToString());
    }
}
