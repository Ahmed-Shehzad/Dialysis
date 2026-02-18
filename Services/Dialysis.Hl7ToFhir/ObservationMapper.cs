using Hl7.Fhir.Model;

namespace Dialysis.Hl7ToFhir;

/// <summary>
/// Maps PCD-01 device observations to FHIR Observation.
/// Per QI-Core NonPatient pattern: Observation with focus → Device.
/// </summary>
public static class ObservationMapper
{
    /// <summary>
    /// Map a device observation (OBX from ORU^R01) to FHIR Observation.
    /// </summary>
    public static Observation ToFhirObservation(ObservationMappingInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var obs = new Observation
        {
            Status = ObservationStatus.Final,
            Code = new CodeableConcept
            {
                Coding =
                [
                    new Coding("urn:iso:std:iso:11073:10101", input.ObservationCode, null)
                ]
            },
            Category =
            [
                new CodeableConcept("http://terminology.hl7.org/CodeSystem/observation-category", "device", "Device")
            ],
            Effective = input.EffectiveTime.HasValue ? new FhirDateTime(input.EffectiveTime.Value) : new FhirDateTime(DateTimeOffset.UtcNow)
        };

        if (!string.IsNullOrEmpty(input.Value))
        {
            if (decimal.TryParse(input.Value, out decimal numVal))
                obs.Value = new Quantity { Value = numVal, Unit = input.Unit ?? string.Empty, System = "http://unitsofmeasure.org", Code = input.Unit };
            else
                obs.Value = new FhirString(input.Value);
        }

        if (!string.IsNullOrEmpty(input.SubId))
            obs.BodySite = new CodeableConcept { Text = input.SubId };

        if (!string.IsNullOrEmpty(input.Provenance)) obs.Note = [new Annotation { Text = $"Provenance: {input.Provenance}" }];

        if (!string.IsNullOrEmpty(input.PatientId))
            obs.Subject = new ResourceReference($"Patient/{input.PatientId}");

        if (!string.IsNullOrEmpty(input.DeviceId))
            obs.Device = new ResourceReference($"Device/{input.DeviceId}");

        return obs;
    }
}
