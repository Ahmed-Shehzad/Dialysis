using BuildingBlocks.Caching;
using BuildingBlocks.Tenancy;

using Dialysis.Patient.Application.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Patient.Application.Features.RegisterPatient;

internal sealed class RegisterPatientCommandHandler : ICommandHandler<RegisterPatientCommand, RegisterPatientResponse>
{
    private const string PatientKeyPrefix = "patient";

    private readonly IPatientRepository _repository;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly ITenantContext _tenant;

    public RegisterPatientCommandHandler(IPatientRepository repository, ICacheInvalidator cacheInvalidator, ITenantContext tenant)
    {
        _repository = repository;
        _cacheInvalidator = cacheInvalidator;
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

        await InvalidatePatientCacheAsync(patient.MedicalRecordNumber.Value, patient.Id.ToString(), cancellationToken);

        return new RegisterPatientResponse(patient.Id.ToString());
    }

    private async Task InvalidatePatientCacheAsync(string mrn, string id, CancellationToken cancellationToken)
    {
        string[] keys = new[] { $"{_tenant.TenantId}:{PatientKeyPrefix}:{mrn}", $"{_tenant.TenantId}:{PatientKeyPrefix}:id:{id}" };
        await _cacheInvalidator.InvalidateAsync(keys, cancellationToken);
    }
}
