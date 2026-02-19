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
        IReadOnlyList<PatientDomain> patients = await ResolvePatientsAsync(request, cancellationToken);
        var summaries = patients.Select(p => new PatientSummary(
            p.Id.ToString(),
            p.MedicalRecordNumber.Value,
            p.Name.FirstName,
            p.Name.LastName,
            p.DateOfBirth,
            p.Gender?.Value)).ToList();
        return new GetPatientsResponse(summaries);
    }

    private async Task<IReadOnlyList<PatientDomain>> ResolvePatientsAsync(GetPatientsQuery request, CancellationToken cancellationToken)
    {
        if (TryGetById(request.Id, out Ulid id))
        {
            PatientDomain? single = await _repository.GetAsync(p => p.TenantId == _tenant.TenantId && p.Id == id, cancellationToken);
            return single is not null ? [single] : [];
        }
        if (HasSearchFilters(request))
        {
            ParseName(request.Name, out string? family, out string? given);
            return await _repository.SearchForFhirAsync(request.Identifier, family, given, request.Birthdate, request.Limit, cancellationToken);
        }
        return await _repository.GetAllForTenantAsync(request.Limit, cancellationToken);
    }

    private static bool TryGetById(string? id, out Ulid parsedId)
    {
        parsedId = default;
        return !string.IsNullOrWhiteSpace(id) && Ulid.TryParse(id, out parsedId);
    }

    private static bool HasSearchFilters(GetPatientsQuery request) =>
        !string.IsNullOrWhiteSpace(request.Identifier) || !string.IsNullOrWhiteSpace(request.Name) || request.Birthdate.HasValue;

    private static void ParseName(string? name, out string? family, out string? given)
    {
        family = null;
        given = null;
        if (string.IsNullOrWhiteSpace(name)) return;
        string[] parts = name.Split('|', StringSplitOptions.TrimEntries);
        family = parts.Length > 0 ? parts[0] : null;
        given = parts.Length > 1 ? parts[1] : null;
    }
}
