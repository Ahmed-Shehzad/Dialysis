using System.Runtime.CompilerServices;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.HIS.PatientFlow.Ports;
using Hl7.Fhir.Model;

namespace Dialysis.HIS.PatientFlow.Fhir;

/// <summary>
/// Emits a minimal FHIR <c>Patient</c> stub for each distinct patient referenced by a HIS
/// Admission. EHR remains the patient-identity system of record — the EHR-side Patient feeder
/// owns demographics, MRN, etc. HIS's stub gives Bulk Data <c>$export</c> consumers (payers,
/// regional HIE pipelines) a complete patient list scoped to HIS activity so cross-resource
/// joins inside the NDJSON output work without round-tripping to EHR mid-export.
/// </summary>
public sealed class HisPatientStubFeeder : INdjsonResourceFeeder<Patient>
{
    private readonly IAdmissionRepository _admissions;
    /// <summary>
    /// Emits a minimal FHIR <c>Patient</c> stub for each distinct patient referenced by a HIS
    /// Admission. EHR remains the patient-identity system of record — the EHR-side Patient feeder
    /// owns demographics, MRN, etc. HIS's stub gives Bulk Data <c>$export</c> consumers (payers,
    /// regional HIE pipelines) a complete patient list scoped to HIS activity so cross-resource
    /// joins inside the NDJSON output work without round-tripping to EHR mid-export.
    /// </summary>
    public HisPatientStubFeeder(IAdmissionRepository admissions) => _admissions = admissions;
    public IAsyncEnumerable<Patient> StreamAsync(ExportJob job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        return StreamCoreAsync(job, cancellationToken);
    }

    private async IAsyncEnumerable<Patient> StreamCoreAsync(
        ExportJob job,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var patientId in _admissions.StreamDistinctPatientIdsAsync(job.Since, cancellationToken).ConfigureAwait(false))
        {
            yield return new Patient
            {
                Id = patientId.ToString(),
                Meta = new Meta
                {
                    Source = "urn:dialysis:his",
                    Tag = [new Coding("urn:dialysis:his:tag", "stub", "HIS patient stub — identity authoritative in EHR")],
                },
                Identifier =
                [
                    new Identifier
                    {
                        System = "urn:dialysis:patient-id",
                        Value = patientId.ToString(),
                        Use = Identifier.IdentifierUse.Usual,
                    },
                ],
            };
        }
    }
}
