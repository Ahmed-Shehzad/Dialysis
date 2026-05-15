using Dialysis.CQRS.Queries;
using Dialysis.EHR.Registration.Ports;

namespace Dialysis.EHR.Registration.Features.SearchPatients;

public sealed class SearchPatientsQueryHandler(IPatientRepository patients)
    : IQueryHandler<SearchPatientsQuery, PatientSearchResult>
{
    public async Task<PatientSearchResult> HandleAsync(
        SearchPatientsQuery request,
        CancellationToken cancellationToken)
    {
        var skip = Math.Max(0, request.Skip);
        var take = Math.Clamp(request.Take, 1, 200);

        var page = await patients
            .SearchAsync(
                new PatientSearchCriteria(
                    request.Query,
                    request.FamilyName,
                    request.GivenName,
                    request.MedicalRecordNumber,
                    request.DateOfBirthFrom,
                    request.DateOfBirthTo,
                    request.SexAtBirthCode,
                    request.Status,
                    skip,
                    take),
                cancellationToken)
            .ConfigureAwait(false);

        var items = page.Items
            .Select(p => new PatientSummary(
                p.Id,
                p.MedicalRecordNumber,
                p.Name.FamilyName,
                p.Name.GivenName,
                p.DateOfBirth,
                p.SexAtBirthCode,
                p.Status.ToString()))
            .ToList();

        return new PatientSearchResult(items, page.TotalCount, skip, take);
    }
}
