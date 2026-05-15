using System.Runtime.CompilerServices;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.EHR.PatientChart.Ports;
using Hl7.Fhir.Model;

namespace Dialysis.EHR.PatientChart.Fhir;

/// <summary>
/// Streams every <c>VitalSignReading</c> as a FHIR R4 <c>Observation</c>. The reading's
/// LOINC-coded observation type maps to <c>Observation.code</c>; the value + unit form a
/// UCUM-coded <c>Quantity</c>.
/// </summary>
public sealed class EhrVitalSignObservationFeeder(IVitalSignRepository readings) : INdjsonResourceFeeder<Observation>
{
    public async IAsyncEnumerable<Observation> StreamAsync(
        ExportJob job,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        await foreach (var reading in readings.StreamAllAsync(job.Since, cancellationToken).ConfigureAwait(false))
        {
            yield return new Observation
            {
                Id = reading.Id.ToString(),
                Meta = new Meta { LastUpdated = new DateTimeOffset(DateTime.SpecifyKind(reading.ObservedAtUtc, DateTimeKind.Utc)) },
                Status = ObservationStatus.Final,
                Subject = new ResourceReference($"Patient/{reading.PatientId}"),
                Encounter = reading.EncounterId is null ? null : new ResourceReference($"Encounter/{reading.EncounterId}"),
                Code = new CodeableConcept(reading.ObservationType.System, reading.ObservationType.Code, reading.ObservationType.Display),
                Effective = new FhirDateTime(reading.ObservedAtUtc),
                Value = new Quantity
                {
                    Value = reading.Value,
                    Unit = reading.UnitCode,
                    System = "http://unitsofmeasure.org",
                    Code = reading.UnitCode,
                },
                Category =
                [
                    new CodeableConcept(
                        "http://terminology.hl7.org/CodeSystem/observation-category",
                        "vital-signs",
                        "Vital Signs"),
                ],
                Performer = reading.RecordedByProviderId is null
                    ? null
                    : [new ResourceReference($"Practitioner/{reading.RecordedByProviderId}")],
            };
        }
    }
}
