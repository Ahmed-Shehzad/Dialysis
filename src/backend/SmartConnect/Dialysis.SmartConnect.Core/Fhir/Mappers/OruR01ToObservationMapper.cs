using Dialysis.SmartConnect.DataTypes;
using Hl7.Fhir.Model;

namespace Dialysis.SmartConnect.Fhir.Mappers;

/// <summary>
/// Maps an HL7 v2 ORU^R01 OBX segment to a FHIR R4 <c>Observation</c>.
/// OBX-3 carries the observation identifier (LOINC); OBX-5 the value; OBX-6 units; OBX-14 obs time.
/// </summary>
public sealed class OruR01ToObservationMapper : IFhirV2MessageMapper<Observation>
{
    public string TriggerEvent => "ORU^R01";

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
        if (!string.IsNullOrEmpty(raw) && decimal.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture, out var numeric))
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

        return observation;
    }
}
