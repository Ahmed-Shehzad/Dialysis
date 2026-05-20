using Dialysis.CQRS.Queries;
using Dialysis.EHR.Registration.Ports;

namespace Dialysis.EHR.Registration.Features.GetPatientById;

public sealed class GetPatientByIdQueryHandler(IPatientRepository patients)
    : IQueryHandler<GetPatientByIdQuery, PatientDetailDto?>
{
    public async Task<PatientDetailDto?> HandleAsync(
        GetPatientByIdQuery request,
        CancellationToken cancellationToken)
    {
        var patient = await patients.GetAsync(request.PatientId, cancellationToken).ConfigureAwait(false);
        if (patient is null) return null;
        return new PatientDetailDto(
            patient.Id,
            patient.MedicalRecordNumber,
            patient.Name.FamilyName,
            patient.Name.GivenName,
            patient.Name.MiddleName,
            patient.DateOfBirth,
            patient.SexAtBirthCode,
            patient.PreferredLanguageCode,
            patient.Status.ToString());
    }
}
