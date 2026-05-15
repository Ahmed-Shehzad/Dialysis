using Dialysis.CQRS.Queries;
using Dialysis.EHR.Registration.Ports;

namespace Dialysis.EHR.Registration.Features.SearchPatients;

public sealed class SearchPatientsQueryHandler(IPatientRepository patients)
    : IQueryHandler<SearchPatientsQuery, IReadOnlyList<PatientSummary>>
{
    public async Task<IReadOnlyList<PatientSummary>> HandleAsync(
        SearchPatientsQuery request,
        CancellationToken cancellationToken)
    {
        var data = await patients
            .SearchAsync(request.Query, request.Take, cancellationToken)
            .ConfigureAwait(false);
        return data
            .Select(p => new PatientSummary(
                p.Id,
                p.MedicalRecordNumber,
                p.Name.FamilyName,
                p.Name.GivenName,
                p.DateOfBirth,
                p.SexAtBirthCode,
                p.Status.ToString()))
            .ToList();
    }
}
