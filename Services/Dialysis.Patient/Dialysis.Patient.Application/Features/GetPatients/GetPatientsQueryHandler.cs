using BuildingBlocks.Tenancy;

using Dialysis.Patient.Application.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Patient.Application.Features.GetPatients;

internal sealed class GetPatientsQueryHandler : IQueryHandler<GetPatientsQuery, GetPatientsResponse>
{
    private readonly IPatientReadStore _readStore;
    private readonly ITenantContext _tenant;

    public GetPatientsQueryHandler(IPatientReadStore readStore, ITenantContext tenant)
    {
        _readStore = readStore;
        _tenant = tenant;
    }

    public async Task<GetPatientsResponse> HandleAsync(GetPatientsQuery request, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PatientReadDto> patients = await ResolvePatientsAsync(request, cancellationToken);
        var summaries = patients.Select(p => new PatientSummary(
            p.Id,
            p.MedicalRecordNumber,
            p.FirstName,
            p.LastName,
            p.DateOfBirth,
            p.Gender)).ToList();
        return new GetPatientsResponse(summaries);
    }

    private async Task<IReadOnlyList<PatientReadDto>> ResolvePatientsAsync(GetPatientsQuery request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Id))
        {
            PatientReadDto? single = await _readStore.GetByIdAsync(_tenant.TenantId, request.Id, cancellationToken);
            return single is not null ? [single] : [];
        }
        if (HasSearchFilters(request))
        {
            ParseName(request.Name, out string? family, out string? given);
            return await _readStore.SearchAsync(_tenant.TenantId, request.Identifier, family, given, request.Birthdate, request.Limit, cancellationToken);
        }
        return await _readStore.GetAllForTenantAsync(_tenant.TenantId, request.Limit, cancellationToken);
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
