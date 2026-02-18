using Hl7.Fhir.Model;

namespace Dialysis.Hl7ToFhir;

/// <summary>
/// Maps PCD-01 device observations to FHIR Observation.
/// Per QI-Core: Observation with focus → Device for device observations.
/// MDC system: urn:iso:std:iso:11073:10101.
/// UCUM system: http://unitsofmeasure.org.
/// </summary>
public static class ObservationMapper
{
    private const string MdcSystem = "urn:iso:std:iso:11073:10101";
    private const string UcumSystem = "http://unitsofmeasure.org";
    private const string LoincSystem = "http://loinc.org";

    /// <summary>
    /// Map a device observation (OBX from ORU^R01) to FHIR Observation.
    /// </summary>
    public static Observation ToFhirObservation(ObservationMappingInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        MdcFhirDescriptor? catalogEntry = MdcToFhirCodeCatalog.Get(input.ObservationCode);
        string displayText = catalogEntry?.DisplayName ?? input.ObservationCode;

        var code = new CodeableConcept
        {
            Coding =
            [
                new Coding(MdcSystem, input.ObservationCode, displayText)
            ],
            Text = displayText
        };

        if (catalogEntry?.LoincCode is not null)
            code.Coding.Add(new Coding(LoincSystem, catalogEntry.LoincCode, displayText));

        var obs = new Observation
        {
            Status = ObservationStatus.Final,
            Code = code,
            Category =
            [
                new CodeableConcept("http://terminology.hl7.org/CodeSystem/observation-category", "device", "Device")
            ],
            Effective = input.EffectiveTime.HasValue
                ? new FhirDateTime(input.EffectiveTime.Value)
                : new FhirDateTime(DateTimeOffset.UtcNow)
        };

        if (!string.IsNullOrEmpty(input.Value))
        {
            if (decimal.TryParse(input.Value, out decimal numVal))
            {
                string ucumCode = UcumMapper.ToUcumCode(input.Unit);
                obs.Value = new Quantity
                {
                    Value = numVal,
                    Unit = input.Unit ?? string.Empty,
                    System = UcumSystem,
                    Code = ucumCode
                };

                if (!string.IsNullOrEmpty(input.ReferenceRange))
                    obs.ReferenceRange = [new Observation.ReferenceRangeComponent { Text = input.ReferenceRange }];
            }
            else obs.Value = new FhirString(input.Value);
        }

        if (!string.IsNullOrEmpty(input.SubId))
            obs.BodySite = new CodeableConcept { Text = input.SubId };

        if (!string.IsNullOrEmpty(input.Provenance))
            obs.Note = [new Annotation { Text = $"Provenance: {input.Provenance}" }];

        if (!string.IsNullOrEmpty(input.PatientId))
            obs.Subject = new ResourceReference($"Patient/{input.PatientId}");

        if (!string.IsNullOrEmpty(input.DeviceId))
            obs.Device = new ResourceReference($"Device/{input.DeviceId}");

        return obs;
    }
}
