using BuildingBlocks.Tenancy;

using Dialysis.Patient.Application.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Patient.Application.Features.SearchPatients;

internal sealed class SearchPatientsQueryHandler : IQueryHandler<SearchPatientsQuery, SearchPatientsResponse>
{
    private readonly IPatientReadStore _readStore;
    private readonly ITenantContext _tenant;

    public SearchPatientsQueryHandler(IPatientReadStore readStore, ITenantContext tenant)
    {
        _readStore = readStore;
        _tenant = tenant;
    }

    public async Task<SearchPatientsResponse> HandleAsync(SearchPatientsQuery request, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PatientReadDto> patients = await _readStore.SearchAsync(
            _tenant.TenantId,
            null,
            request.Name.LastName,
            request.Name.FirstName,
            null,
            1000,
            cancellationToken);
        var matches = patients.Select(p => new PatientMatch(p.Id, p.MedicalRecordNumber, p.FirstName, p.LastName, p.DateOfBirth)).ToList();
        return new SearchPatientsResponse(matches);
    }
}
