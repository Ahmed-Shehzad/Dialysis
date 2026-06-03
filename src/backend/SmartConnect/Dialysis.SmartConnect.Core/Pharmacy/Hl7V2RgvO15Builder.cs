using System.Globalization;
using System.Text;

namespace Dialysis.SmartConnect.Pharmacy;

/// <summary>
/// Builds the HL7 v2.5 chapter 4A <c>RGV^O15^RGV_O15</c> pharmacy/treatment-give message
/// frame from a <see cref="MedicationGiveFrame"/>. RGV is the "give/prepare" trigger; we use
/// it to communicate a clinical <em>decline</em> outbound — the give segment names the ordered
/// product, the order control code <c>DC</c> ("discontinue") signals the dose was not
/// administered, and an NTE refusal note carries the operator's reason verbatim so the
/// receiving pharmacy system can reconcile the unused dose.
///
/// Minimal conformant segment set: MSH, PID, ORC, RXG, RXR, NTE.
/// </summary>
public static class Hl7V2RgvO15Builder
{
    private const string MessageType = "RGV^O15^RGV_O15";
    private const string ProcessingId = "P";
    private const string VersionId = "2.5";

    public static string Build(
        MedicationGiveFrame give,
        string messageControlId,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(give);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageControlId);

        var sb = new StringBuilder();
        AppendMsh(sb, give.SendingApplication, messageControlId, nowUtc);
        AppendPid(sb, give.PatientIdentifier);
        AppendOrc(sb, give.PlacerOrderNumber);
        AppendRxg(sb, give);
        AppendNte(sb, give.Reason);
        return sb.ToString();
    }

    private static void AppendMsh(StringBuilder sb, string sendingApplication, string controlId, DateTime nowUtc)
    {
        sb.Append("MSH|^~\\&|")
          .Append(Sanitize(sendingApplication))
          .Append("||||").Append(FormatTs(nowUtc))
          .Append("||").Append(MessageType).Append('|').Append(Sanitize(controlId)).Append('|')
          .Append(ProcessingId).Append('|').Append(VersionId)
          .Append("|||AL|NE|||||\r");
    }

    private static void AppendPid(StringBuilder sb, string patientIdentifier) =>
        sb.Append("PID|||").Append(Sanitize(patientIdentifier)).Append("^^^^MR\r");

    // ORC-1 = DC (discontinue): the give was cancelled because the patient declined.
    private static void AppendOrc(StringBuilder sb, string? placerOrderNumber) =>
        sb.Append("ORC|DC|").Append(Sanitize(placerOrderNumber ?? string.Empty)).Append("\r");

    private static void AppendRxg(StringBuilder sb, MedicationGiveFrame g)
    {
        // RXG-1 Give Sub-ID (1), RXG-2 Dispense Sub-ID (1), RXG-3 quantity/timing
        // (^^^<give-ts> — start time only), RXG-4 give code (CWE). Amount/units are left
        // empty: a declined dose was never measured out.
        var ts = FormatTs(g.GiveAtUtc);
        sb.Append("RXG|1|1|^^^").Append(ts).Append('|')
          .Append(FormatCwe(g.Medication))
          .Append("\r");
    }

    private static void AppendNte(StringBuilder sb, string reason) =>
        sb.Append("NTE|1|RE|").Append(Sanitize(reason)).Append("\r");

    private static string FormatCwe(PharmacyMedication med) =>
        $"{Sanitize(med.Code)}^{Sanitize(med.Display)}^{Sanitize(med.System)}";

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
