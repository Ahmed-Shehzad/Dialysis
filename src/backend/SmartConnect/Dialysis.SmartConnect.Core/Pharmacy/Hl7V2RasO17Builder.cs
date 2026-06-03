using System.Globalization;
using System.Text;

namespace Dialysis.SmartConnect.Pharmacy;

/// <summary>
/// Builds the HL7 v2.5 chapter 4A <c>RAS^O17^RAS_O17</c> pharmacy/treatment-administration
/// message frame from a <see cref="MedicationAdministrationFrame"/>. RAS reports that a dose
/// was actually given to the patient — distinct from RDE (encode) / RGV (give/prepare). The
/// minimal conformant segment set is MSH, PID, ORC, RXA, RXR:
/// <list type="bullet">
///   <item>MSH — message header (sending app = the dialysis platform).</item>
///   <item>PID — patient identifier (MR identifier type).</item>
///   <item>ORC — common order; control code <c>RE</c> ("observations/performed") since the
///         administration is a fait-accompli being reported outbound.</item>
///   <item>RXA — pharmacy administration: administered code (RXA-5), amount (RXA-6), units
///         (RXA-7), start/end timestamps (RXA-3/4), administering provider (RXA-10),
///         completion status <c>CP</c> (RXA-20) and action code <c>A</c> (RXA-21).</item>
///   <item>RXR — route of administration (RXR-1).</item>
/// </list>
/// Delimiters follow the HL7 convention encoded in MSH-1/2 (<c>|^~\&amp;</c>); the builder
/// sanitises every interpolated value so a stray delimiter in operator-entered text can't
/// corrupt the frame.
/// </summary>
public static class Hl7V2RasO17Builder
{
    private const string MessageType = "RAS^O17^RAS_O17";
    private const string ProcessingId = "P";
    private const string VersionId = "2.5";
    private const string RouteCodingSystem = "HL70162";

    public static string Build(
        MedicationAdministrationFrame administration,
        string messageControlId,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(administration);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageControlId);

        var sb = new StringBuilder();
        AppendMsh(sb, administration.SendingApplication, MessageType, messageControlId, nowUtc);
        AppendPid(sb, administration.PatientIdentifier);
        AppendOrc(sb, administration.PlacerOrderNumber);
        AppendRxa(sb, administration);
        AppendRxr(sb, administration.Route);
        return sb.ToString();
    }

    private static void AppendMsh(StringBuilder sb, string sendingApplication, string messageType, string controlId, DateTime nowUtc)
    {
        sb.Append("MSH|^~\\&|")
          .Append(Sanitize(sendingApplication))
          .Append("||||").Append(FormatTs(nowUtc))
          .Append("||").Append(messageType).Append('|').Append(Sanitize(controlId)).Append('|')
          .Append(ProcessingId).Append('|').Append(VersionId)
          .Append("|||AL|NE|||||\r");
    }

    private static void AppendPid(StringBuilder sb, string patientIdentifier) =>
        sb.Append("PID|||").Append(Sanitize(patientIdentifier)).Append("^^^^MR\r");

    private static void AppendOrc(StringBuilder sb, string? placerOrderNumber) =>
        sb.Append("ORC|RE|").Append(Sanitize(placerOrderNumber ?? string.Empty)).Append("\r");

    private static void AppendRxa(StringBuilder sb, MedicationAdministrationFrame a)
    {
        // RXA-1 Give Sub-ID (0), RXA-2 Administration Sub-ID (1), RXA-3 start, RXA-4 end
        // (same instant for a bolus), RXA-5 administered code (CWE), RXA-6 amount,
        // RXA-7 units (CWE), RXA-9 admin notes (blank), RXA-10 administering provider,
        // RXA-20 completion status CP, RXA-21 action code A.
        var ts = FormatTs(a.AdministeredAtUtc);
        sb.Append("RXA|0|1|").Append(ts).Append('|').Append(ts).Append('|')
          .Append(FormatCwe(a.Medication)).Append('|')
          .Append(FormatAmount(a.DoseQuantity)).Append('|')
          .Append(Sanitize(a.DoseUnit)).Append("^").Append(Sanitize(a.DoseUnit)).Append("^UCUM")
          .Append("|||")
          .Append(Sanitize(a.AdministeredBy))
          .Append("|||||||||CP|A\r");
    }

    private static void AppendRxr(StringBuilder sb, string route) =>
        sb.Append("RXR|").Append(Sanitize(route)).Append('^').Append(Sanitize(route)).Append('^').Append(RouteCodingSystem).Append("\r");

    private static string FormatCwe(PharmacyMedication med) =>
        $"{Sanitize(med.Code)}^{Sanitize(med.Display)}^{Sanitize(med.System)}";

    private static string FormatAmount(decimal value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string FormatTs(DateTime utc) =>
        utc.ToUniversalTime().ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + "+0000";

    private static string Sanitize(string value) =>
        value
            .Replace("|", string.Empty, StringComparison.Ordinal)
            .Replace("^", string.Empty, StringComparison.Ordinal)
            .Replace("~", string.Empty, StringComparison.Ordinal)
            .Replace("\\", string.Empty, StringComparison.Ordinal)
            .Replace("&", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
}
