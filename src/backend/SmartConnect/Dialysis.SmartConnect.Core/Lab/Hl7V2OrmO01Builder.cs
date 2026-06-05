using System.Globalization;
using System.Text;

namespace Dialysis.SmartConnect.Lab;

/// <summary>
/// Builds the HL7 v2.5 chapter 4 <c>ORM^O01^ORM_O01</c> general-order message frame from a
/// <see cref="LabOrderFrame"/>. ORM^O01 places a new laboratory order with one or more requested
/// services. The minimal conformant segment set is MSH, PID, then one <c>ORC + OBR</c> group per
/// requested test:
/// <list type="bullet">
///   <item>MSH — message header (sending app = the dialysis platform).</item>
///   <item>PID — patient identifier (MR identifier type).</item>
///   <item>ORC — common order; control code <c>NW</c> ("new order"). The placer order number
///         (ORC-2) is our stable identity for matching the returned ORU result. STAT priority
///         rides ORC-7 (Quantity/Timing) priority component.</item>
///   <item>OBR — observation request: set-id (OBR-1), placer order number (OBR-2), universal
///         service identifier (OBR-4, LOINC CWE), and the specimen source (OBR-15).</item>
/// </list>
/// Delimiters follow the HL7 convention encoded in MSH-1/2 (<c>|^~\&amp;</c>); the builder
/// sanitises every interpolated value so a stray delimiter in operator-entered text can't corrupt
/// the frame. Fields are assembled by index and joined with <c>|</c> so segment offsets stay
/// correct regardless of how many trailing components are populated.
/// </summary>
public static class Hl7V2OrmO01Builder
{
    private const string MessageType = "ORM^O01^ORM_O01";
    private const string ProcessingId = "P";
    private const string VersionId = "2.5";

    /// <summary>STAT orders carry priority <c>S</c>; routine orders <c>R</c> (HL7 table 0027).</summary>
    public static string Build(LabOrderFrame order, string messageControlId, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageControlId);

        var sb = new StringBuilder();
        AppendMsh(sb, order.SendingApplication, messageControlId, nowUtc);
        AppendPid(sb, order.PatientIdentifier);

        var priorityCode = order.IsStat ? "S" : "R";
        var orderedTs = FormatTs(order.OrderedAtUtc);
        var setId = 1;
        foreach (var test in order.Tests)
        {
            AppendOrc(sb, order.PlacerOrderNumber, priorityCode, orderedTs);
            AppendObr(sb, setId++, order.PlacerOrderNumber, test, order.Specimen);
        }

        return sb.ToString();
    }

    private static void AppendMsh(StringBuilder sb, string sendingApplication, string controlId, DateTime nowUtc) =>
        sb.Append("MSH|^~\\&|")
          .Append(Sanitize(sendingApplication))
          .Append("||||").Append(FormatTs(nowUtc))
          .Append("||").Append(MessageType).Append('|').Append(Sanitize(controlId)).Append('|')
          .Append(ProcessingId).Append('|').Append(VersionId)
          .Append("|||AL|NE|||||\r");

    private static void AppendPid(StringBuilder sb, string patientIdentifier) =>
        sb.Append("PID|||").Append(Sanitize(patientIdentifier)).Append("^^^^MR\r");

    private static void AppendOrc(StringBuilder sb, string placerOrderNumber, string priorityCode, string orderedTs)
    {
        // ORC-7 Quantity/Timing: priority lives in component 6 (`^^^^^S`).
        // ORC-9 date/time of transaction carries the order-authored timestamp.
        var fields = new string[10];
        fields[0] = "ORC";
        fields[1] = "NW";
        fields[2] = Sanitize(placerOrderNumber);
        fields[7] = "^^^^^" + priorityCode;
        fields[9] = orderedTs;
        var line = string.Join('|', fields).TrimEnd('|');
        sb.Append(line).Append("\r");
    }

    private static void AppendObr(StringBuilder sb, int setId, string placerOrderNumber, LabTestRequest test, string? specimen)
    {
        // OBR-4 universal service id (LOINC CWE); OBR-15 specimen source.
        var fields = new string[16];
        fields[0] = "OBR";
        fields[1] = setId.ToString(CultureInfo.InvariantCulture);
        fields[2] = Sanitize(placerOrderNumber);
        fields[4] = FormatCwe(test);
        fields[15] = string.IsNullOrWhiteSpace(specimen) ? string.Empty : Sanitize(specimen);
        var line = string.Join('|', fields).TrimEnd('|');
        sb.Append(line).Append("\r");
    }

    private static string FormatCwe(LabTestRequest test) =>
        $"{Sanitize(test.Code)}^{Sanitize(test.Display)}^{Sanitize(test.System)}";

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
