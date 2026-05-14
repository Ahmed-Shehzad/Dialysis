using Dialysis.SmartConnect.DataTypes;
using Hl7.Fhir.Model;

namespace Dialysis.SmartConnect.Fhir.Mappers;

/// <summary>
/// Maps an HL7 v2 ADT^A01 PV1 segment to a FHIR R4 <c>Encounter</c>.
/// PV1-1 controls the set id; PV1-2 the patient class; PV1-3 location; PV1-44 admit date.
/// </summary>
public sealed class AdtA01ToEncounterMapper : IFhirV2MessageMapper<Encounter>
{
    public string TriggerEvent => "ADT^A01";

    public Encounter Map(Hl7V2Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var encounter = new Encounter
        {
            Status = Encounter.EncounterStatus.InProgress,
            Class = MapClass(message.GetValue("PV1.2")),
        };

        var visitNumber = message.GetValue("PV1.19.1");
        if (!string.IsNullOrEmpty(visitNumber))
        {
            encounter.Id = visitNumber;
            encounter.Identifier.Add(new Identifier(system: "urn:dialysis:visit", value: visitNumber));
        }

        var mrn = message.GetValue("PID.3.1");
        if (!string.IsNullOrEmpty(mrn))
        {
            encounter.Subject = new ResourceReference($"Patient/{mrn}");
        }

        var location = message.GetValue("PV1.3.1");
        if (!string.IsNullOrEmpty(location))
        {
            encounter.Location.Add(new Encounter.LocationComponent
            {
                Location = new ResourceReference($"Location/{location}"),
            });
        }

        var admit = message.GetValue("PV1.44");
        if (!string.IsNullOrEmpty(admit) && admit.Length >= 8)
        {
            encounter.Period = new Period
            {
                StartElement = new FhirDateTime($"{admit[..4]}-{admit.Substring(4, 2)}-{admit.Substring(6, 2)}"),
            };
        }

        return encounter;
    }

    private static Coding MapClass(string? pv1Class) => pv1Class switch
    {
        "I" => new Coding("http://terminology.hl7.org/CodeSystem/v3-ActCode", "IMP", "inpatient encounter"),
        "O" => new Coding("http://terminology.hl7.org/CodeSystem/v3-ActCode", "AMB", "ambulatory"),
        "E" => new Coding("http://terminology.hl7.org/CodeSystem/v3-ActCode", "EMER", "emergency"),
        _ => new Coding("http://terminology.hl7.org/CodeSystem/v3-ActCode", "AMB", "ambulatory"),
    };
}
