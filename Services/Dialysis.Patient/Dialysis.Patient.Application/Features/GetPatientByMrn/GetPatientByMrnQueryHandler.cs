using Dialysis.Patient.Application.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Patient.Application.Features.GetPatientByMrn;

internal sealed class GetPatientByMrnQueryHandler : IQueryHandler<GetPatientByMrnQuery, GetPatientByMrnResponse?>
{
    private readonly IPatientRepository _repository;

    public GetPatientByMrnQueryHandler(IPatientRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetPatientByMrnResponse?> HandleAsync(GetPatientByMrnQuery request, CancellationToken cancellationToken = default)
    {
        var patient = await _repository.GetByMrnAsync(request.Mrn, cancellationToken);
        return patient is null
            ? null
            : new GetPatientByMrnResponse(
                patient.Id.ToString(),
                patient.MedicalRecordNumber,
                patient.Name.FirstName,
                patient.Name.LastName,
                patient.DateOfBirth,
                patient.Gender?.Value);
    }
}
