using System.Globalization;
using Dialysis.SmartConnect.DataTypes;
using Hl7.Fhir.Model;

namespace Dialysis.SmartConnect.Fhir.Mappers;

/// <summary>
/// Maps an HL7 v2 SIU^S12 (new appointment booking) to a FHIR R4 <c>Appointment</c>.
/// SCH-1 carries the placer appointment id; SCH-11.4 the appointment start time; SCH-25 the
/// appointment status; PID-3 the patient identifier.
/// </summary>
public sealed class SiuS12ToAppointmentMapper : IFhirV2MessageMapper<Appointment>
{
    public string TriggerEvent => "SIU^S12";

    public Appointment Map(Hl7V2Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var appointment = new Appointment
        {
            Status = MapStatus(message.GetValue("SCH.25")),
        };

        var schedId = message.GetValue("SCH.1.1");
        if (!string.IsNullOrEmpty(schedId))
        {
            appointment.Identifier.Add(new Identifier
            {
                System = "urn:ietf:rfc:3986",
                Value = schedId,
            });
        }

        var start = message.GetValue("SCH.11.4");
        if (TryParseHl7Timestamp(start, out var startInstant))
        {
            appointment.Start = startInstant;

            // SCH-11.2 (duration) + SCH-11.3 (duration units) feed the End calculation when present.
            // We default to a 30-minute window so the resource is well-formed even with a missing duration.
            var durationMinutes = 30;
            var durationRaw = message.GetValue("SCH.11.2");
            if (!string.IsNullOrEmpty(durationRaw) && int.TryParse(durationRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedDur) && parsedDur > 0)
            {
                durationMinutes = parsedDur;
            }
            appointment.End = startInstant.AddMinutes(durationMinutes);
        }

        var mrn = message.GetValue("PID.3.1");
        if (!string.IsNullOrEmpty(mrn))
        {
            appointment.Participant.Add(new Appointment.ParticipantComponent
            {
                Actor = new ResourceReference($"Patient/{mrn}"),
                Status = ParticipationStatus.Accepted,
            });
        }

        return appointment;
    }

    private static Appointment.AppointmentStatus MapStatus(string? sch25) =>
        (sch25 ?? string.Empty).ToUpper(CultureInfo.InvariantCulture) switch
        {
            "B" => Appointment.AppointmentStatus.Booked,
            "P" => Appointment.AppointmentStatus.Pending,
            "C" => Appointment.AppointmentStatus.Cancelled,
            "X" => Appointment.AppointmentStatus.Noshow,
            "F" => Appointment.AppointmentStatus.Fulfilled,
            _ => Appointment.AppointmentStatus.Booked,
        };

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
