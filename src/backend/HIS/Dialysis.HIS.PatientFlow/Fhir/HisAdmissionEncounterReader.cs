using Dialysis.BuildingBlocks.Fhir;
using Dialysis.HIS.PatientFlow.Domain;
using Dialysis.HIS.PatientFlow.Ports;
using Hl7.Fhir.Model;

namespace Dialysis.HIS.PatientFlow.Fhir;

/// <summary>
/// Projects an HIS <see cref="Admission"/> aggregate as a FHIR R4 <c>Encounter</c> resource.
/// Identifier reuses the admission Guid; status reflects admit/discharge state; subject
/// references the EHR-owned <c>Patient/{patientId}</c>.
/// </summary>
public sealed class HisAdmissionEncounterReader(IAdmissionRepository admissions) : IFhirReader<Encounter>
{
    public async ValueTask<FhirReadResult<Encounter>> ReadAsync(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var admissionId))
            return new FhirReadResult<Encounter>(null);

        var admission = await admissions.GetAsync(admissionId, cancellationToken).ConfigureAwait(false);
        if (admission is null)
            return new FhirReadResult<Encounter>(null);

        var encounter = new Encounter
        {
            Id = admission.Id.ToString(),
            Status = admission.DischargedAtUtc is null
                ? Encounter.EncounterStatus.InProgress
                : Encounter.EncounterStatus.Finished,
            Subject = new ResourceReference($"Patient/{admission.PatientId}"),
            Period = new Period
            {
                StartElement = new FhirDateTime(admission.AdmittedAtUtc),
                EndElement = admission.DischargedAtUtc is { } dischargedAt
                    ? new FhirDateTime(dischargedAt)
                    : null,
            },
            Location =
            [
                new Encounter.LocationComponent
                {
                    Location = new ResourceReference($"Location/{admission.Ward.Value}"),
                },
            ],
        };

        return new FhirReadResult<Encounter>(
            encounter,
            VersionId: "1",
            LastModified: admission.DischargedAtUtc ?? admission.AdmittedAtUtc);
    }
}
