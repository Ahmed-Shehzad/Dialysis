using Dialysis.Patient.Application.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Patient.Application.Features.SearchPatients;

internal sealed class SearchPatientsQueryHandler : IQueryHandler<SearchPatientsQuery, SearchPatientsResponse>
{
    private readonly IPatientRepository _repository;

    public SearchPatientsQueryHandler(IPatientRepository repository)
    {
        _repository = repository;
    }

    public async Task<SearchPatientsResponse> HandleAsync(SearchPatientsQuery request, CancellationToken cancellationToken = default)
    {
        var patients = await _repository.SearchByNameAsync(request.Name, cancellationToken);
        var matches = patients.Select(p => new PatientMatch(
            p.Id.ToString(),
            p.MedicalRecordNumber,
            p.Name.FirstName,
            p.Name.LastName,
            p.DateOfBirth)).ToList();
        return new SearchPatientsResponse(matches);
    }
}
