using Dialysis.SmartConnect.DataTypes;
using Hl7.Fhir.Model;

namespace Dialysis.SmartConnect.Fhir.Mappers;

/// <summary>
/// Maps an HL7 v2 ADT^A40 (merge patient — patient identifier list) to a FHIR R4 <c>Patient</c>.
/// PID-3 carries the surviving (correct) identifier; MRG-1 carries the prior identifier(s) being
/// merged in. The mapper emits the surviving Patient resource and records the merge by adding
/// a <c>Link</c> of type <see cref="Patient.LinkType.Replaces"/> pointing at each prior identifier.
/// Downstream services interpret this as "Patient/{mrg} has been superseded by Patient/{pid3}".
/// </summary>
public sealed class AdtA40ToPatientMapper : IFhirV2MessageMapper<Patient>
{
    public string TriggerEvent => "ADT^A40";

    public Patient Map(Hl7V2Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var patient = new Patient();
        var surviving = message.GetValue("PID.3.1");
        if (!string.IsNullOrEmpty(surviving))
        {
            patient.Id = surviving;
            patient.Identifier.Add(new Identifier(system: "urn:dialysis:mrn", value: surviving));
        }

        var prior = message.GetValue("MRG.1.1");
        if (!string.IsNullOrEmpty(prior))
        {
            patient.Link.Add(new Patient.LinkComponent
            {
                Other = new ResourceReference($"Patient/{prior}"),
                Type = Patient.LinkType.Replaces,
            });
        }

        var family = message.GetValue("PID.5.1");
        var given = message.GetValue("PID.5.2");
        if (!string.IsNullOrEmpty(family) || !string.IsNullOrEmpty(given))
        {
            var name = new HumanName { Family = family };
            if (!string.IsNullOrEmpty(given)) name.GivenElement.Add(new FhirString(given));
            patient.Name.Add(name);
        }

        return patient;
    }
}
