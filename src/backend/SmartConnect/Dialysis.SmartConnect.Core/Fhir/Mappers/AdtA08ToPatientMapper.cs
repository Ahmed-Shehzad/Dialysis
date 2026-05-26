using Dialysis.SmartConnect.DataTypes;
using Hl7.Fhir.Model;

namespace Dialysis.SmartConnect.Fhir.Mappers;

/// <summary>
/// Maps an HL7 v2 ADT^A08 (update patient information) PID segment to a FHIR R4 <c>Patient</c>.
/// Consumers downstream replace the existing record by <c>Patient.id</c>; the shape is the same as
/// ADT^A01 so the downstream merge logic is uniform.
/// </summary>
public sealed class AdtA08ToPatientMapper : IFhirV2MessageMapper<Patient>
{
    public string TriggerEvent => "ADT^A08";

    public Patient Map(Hl7V2Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var patient = new Patient();
        var mrn = message.GetValue("PID.3.1");
        if (!string.IsNullOrEmpty(mrn))
        {
            patient.Id = mrn;
            patient.Identifier.Add(new Identifier(system: "urn:dialysis:mrn", value: mrn));
        }

        var family = message.GetValue("PID.5.1");
        var given = message.GetValue("PID.5.2");
        var middle = message.GetValue("PID.5.3");
        if (!string.IsNullOrEmpty(family) || !string.IsNullOrEmpty(given))
        {
            var name = new HumanName { Family = family };
            if (!string.IsNullOrEmpty(given)) name.GivenElement.Add(new FhirString(given));
            if (!string.IsNullOrEmpty(middle)) name.GivenElement.Add(new FhirString(middle));
            patient.Name.Add(name);
        }

        var birth = message.GetValue("PID.7");
        if (!string.IsNullOrEmpty(birth) && birth.Length >= 8)
        {
            patient.BirthDate = $"{birth[..4]}-{birth.Substring(4, 2)}-{birth.Substring(6, 2)}";
        }

        var sex = message.GetValue("PID.8");
        patient.Gender = sex switch
        {
            "M" => AdministrativeGender.Male,
            "F" => AdministrativeGender.Female,
            "O" => AdministrativeGender.Other,
            "U" => AdministrativeGender.Unknown,
            _ => null,
        };

        // Mark explicitly as update vs new — useful for downstream merge logic.
        patient.Meta = new Meta { Tag = { new Coding("http://dialysis.local/tags", "patient-update") } };

        return patient;
    }
}
