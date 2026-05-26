using System.Globalization;
using Dialysis.SmartConnect.DataTypes;
using Hl7.Fhir.Model;

namespace Dialysis.SmartConnect.Fhir.Mappers;

/// <summary>
/// Maps an HL7 v2 VXU^V04 (unsolicited vaccination record update) to a FHIR R4 <c>Immunization</c>.
/// RXA-3 carries the administration timestamp; RXA-5 the vaccine code (CVX); PID-3 the patient id.
/// </summary>
public sealed class VxuV04ToImmunizationMapper : IFhirV2MessageMapper<Immunization>
{
    public string TriggerEvent => "VXU^V04";

    public Immunization Map(Hl7V2Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var imm = new Immunization
        {
            Status = Immunization.ImmunizationStatusCodes.Completed,
            VaccineCode = new CodeableConcept
            {
                Coding =
                [
                    new Coding(
                        system: "http://hl7.org/fhir/sid/cvx",
                        code: message.GetValue("RXA.5.1") ?? "unknown",
                        display: message.GetValue("RXA.5.2")),
                ],
            },
        };

        var mrn = message.GetValue("PID.3.1");
        if (!string.IsNullOrEmpty(mrn))
        {
            imm.Patient = new ResourceReference($"Patient/{mrn}");
        }

        var occurred = message.GetValue("RXA.3");
        if (TryParseHl7Timestamp(occurred, out var instant))
        {
            imm.Occurrence = new FhirDateTime(instant);
        }

        var lotNumber = message.GetValue("RXA.15");
        if (!string.IsNullOrEmpty(lotNumber))
        {
            imm.LotNumber = lotNumber;
        }

        return imm;
    }

    private static bool TryParseHl7Timestamp(string? raw, out DateTimeOffset value)
    {
        value = default;
        if (string.IsNullOrEmpty(raw) || raw.Length < 8)
        {
            return false;
        }

        var year = int.Parse(raw[..4], CultureInfo.InvariantCulture);
        var month = int.Parse(raw.Substring(4, 2), CultureInfo.InvariantCulture);
        var day = int.Parse(raw.Substring(6, 2), CultureInfo.InvariantCulture);
        var hour = raw.Length >= 10 ? int.Parse(raw.Substring(8, 2), CultureInfo.InvariantCulture) : 0;
        var minute = raw.Length >= 12 ? int.Parse(raw.Substring(10, 2), CultureInfo.InvariantCulture) : 0;

        value = new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero);
        return true;
    }
}
