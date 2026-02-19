using BuildingBlocks.Tenancy;

using Dialysis.Patient.Application.Abstractions;

using Intercessor.Abstractions;

using PatientDomain = Dialysis.Patient.Application.Domain.Patient;

namespace Dialysis.Patient.Application.Features.GetPatients;

internal sealed class GetPatientsQueryHandler : IQueryHandler<GetPatientsQuery, GetPatientsResponse>
{
    private readonly IPatientRepository _repository;
    private readonly ITenantContext _tenant;

    public GetPatientsQueryHandler(IPatientRepository repository, ITenantContext tenant)
    {
        _repository = repository;
        _tenant = tenant;
    }

    public async Task<GetPatientsResponse> HandleAsync(GetPatientsQuery request, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PatientDomain> patients;
        if (!string.IsNullOrWhiteSpace(request.Id) && Ulid.TryParse(request.Id, out Ulid id))
        {
            var single = await _repository.GetAsync(p => p.TenantId == _tenant.TenantId && p.Id == id, cancellationToken);
            patients = single is not null ? [single] : [];
        }
        else if (!string.IsNullOrWhiteSpace(request.Identifier) || !string.IsNullOrWhiteSpace(request.Name) || request.Birthdate.HasValue)
        {
            string? family = null;
            string? given = null;
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var parts = request.Name.Split('|', StringSplitOptions.TrimEntries);
                family = parts.Length > 0 ? parts[0] : null;
                given = parts.Length > 1 ? parts[1] : null;
            }
            patients = await _repository.SearchForFhirAsync(
                request.Identifier,
                family,
                given,
                request.Birthdate,
                request.Limit,
                cancellationToken);
        }
        else
        {
            patients = await _repository.GetAllForTenantAsync(request.Limit, cancellationToken);
        }

        var summaries = patients.Select(p => new PatientSummary(
            p.Id.ToString(),
            p.MedicalRecordNumber.Value,
            p.Name.FirstName,
            p.Name.LastName,
            p.DateOfBirth,
            p.Gender?.Value)).ToList();
        return new GetPatientsResponse(summaries);
    }
}
