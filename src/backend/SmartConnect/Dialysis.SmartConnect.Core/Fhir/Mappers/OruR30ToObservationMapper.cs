using System.Globalization;
using Dialysis.SmartConnect.DataTypes;
using Hl7.Fhir.Model;

namespace Dialysis.SmartConnect.Fhir.Mappers;

/// <summary>
/// Maps an HL7 v2 ORU^R30 (unsolicited point-of-care observation) OBX segment to a FHIR R4
/// <c>Observation</c>. Field semantics match ORU^R01; the trigger differs because R30 represents
/// near-real-time bedside-device observations (e.g. blood pressure, glucose) rather than lab
/// results, and consumers may apply different validation / retention policies based on the trigger.
/// </summary>
public sealed class OruR30ToObservationMapper : IFhirV2MessageMapper<Observation>
{
    public string TriggerEvent => "ORU^R30";

    public Observation Map(Hl7V2Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var observation = new Observation
        {
            Status = ObservationStatus.Final,
            Code = new CodeableConcept
            {
                Coding =
                [
                    new Coding(
                        system: "http://loinc.org",
                        code: message.GetValue("OBX.3.1") ?? "unknown",
                        display: message.GetValue("OBX.3.2")),
                ],
            },
        };

        var mrn = message.GetValue("PID.3.1");
        if (!string.IsNullOrEmpty(mrn))
        {
            observation.Subject = new ResourceReference($"Patient/{mrn}");
        }

        var raw = message.GetValue("OBX.5");
        var unit = message.GetValue("OBX.6.1");
        if (!string.IsNullOrEmpty(raw) && decimal.TryParse(raw, CultureInfo.InvariantCulture, out var numeric))
        {
            observation.Value = new Quantity { Value = numeric, Unit = unit, System = "http://unitsofmeasure.org", Code = unit };
        }
        else if (!string.IsNullOrEmpty(raw))
        {
            observation.Value = new FhirString(raw);
        }

        var time = message.GetValue("OBX.14");
        if (!string.IsNullOrEmpty(time) && time.Length >= 8)
        {
            observation.Effective = new FhirDateTime($"{time[..4]}-{time.Substring(4, 2)}-{time.Substring(6, 2)}");
        }

        // Tag point-of-care provenance — downstream PDMS / EHR consumers prioritise these for
        // near-real-time chairside displays vs. ORU^R01 lab results which arrive on cadence.
        observation.Meta = new Meta { Tag = { new Coding("http://terminology.hl7.org/CodeSystem/v3-ActCode", "POC", "point of care") } };

        return observation;
    }
}
