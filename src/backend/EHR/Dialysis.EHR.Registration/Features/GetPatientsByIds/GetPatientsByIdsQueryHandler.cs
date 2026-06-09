using Dialysis.CQRS.Queries;
using Dialysis.EHR.Registration.Ports;

namespace Dialysis.EHR.Registration.Features.GetPatientsByIds;

public sealed class GetPatientsByIdsQueryHandler
    : IQueryHandler<GetPatientsByIdsQuery, IReadOnlyList<PatientLabelDto>>
{
    private readonly IPatientRepository _patients;
    public GetPatientsByIdsQueryHandler(IPatientRepository patients) => _patients = patients;

    public async Task<IReadOnlyList<PatientLabelDto>> HandleAsync(
        GetPatientsByIdsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.PatientIds.Count == 0) return [];
        var patients = await _patients.GetByIdsAsync(request.PatientIds, cancellationToken).ConfigureAwait(false);
        return
        [
            .. patients.Select(p => new PatientLabelDto(
                p.Id,
                p.MedicalRecordNumber,
                p.Name.FamilyName,
                p.Name.GivenName,
                p.Name.MiddleName,
                p.DateOfBirth)),
        ];
    }
}
