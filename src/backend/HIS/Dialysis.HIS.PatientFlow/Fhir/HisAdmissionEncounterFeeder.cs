using System.Runtime.CompilerServices;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.HIS.PatientFlow.Ports;
using Hl7.Fhir.Model;

namespace Dialysis.HIS.PatientFlow.Fhir;

/// <summary>
/// Streams every HIS <see cref="Domain.Admission"/> as a FHIR <c>Encounter</c> for inclusion
/// in a Bulk Data <c>$export</c>. Honours the job's <c>_since</c> filter against the latest
/// of discharge/admit timestamps.
/// </summary>
public sealed class HisAdmissionEncounterFeeder : INdjsonResourceFeeder<Encounter>
{
    private readonly IAdmissionRepository _admissions;
    /// <summary>
    /// Streams every HIS <see cref="Domain.Admission"/> as a FHIR <c>Encounter</c> for inclusion
    /// in a Bulk Data <c>$export</c>. Honours the job's <c>_since</c> filter against the latest
    /// of discharge/admit timestamps.
    /// </summary>
    public HisAdmissionEncounterFeeder(IAdmissionRepository admissions) => _admissions = admissions;
    public async IAsyncEnumerable<Encounter> StreamAsync(
        ExportJob job,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        await foreach (var admission in _admissions.StreamAllAsync(job.Since, cancellationToken).ConfigureAwait(false))
        {
            yield return new Encounter
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
        }
    }
}
