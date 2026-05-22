using Dialysis.SmartConnect.DataTypes;

namespace Dialysis.SmartConnect.Prescription;

/// <summary>
/// Parses an inbound dialysis-prescription <c>QBP^Q22^QBP_Q21</c> query (IG §5.2.1)
/// into a <see cref="PrescriptionQuery"/>. The query is identified by its QPD-1 name
/// <c>MDC_HDIALY_RX_QUERY</c>; QPD-3 follows the same <c>@PID.x^value^^^^id-type</c>
/// shape as the PDQ message.
/// </summary>
public static class Hl7V2RxQueryParser
{
    /// <summary>The QPD-1 query-name component-2 value that identifies a prescription query.</summary>
    public const string QueryName = "MDC_HDIALY_RX_QUERY";

    /// <summary>
    /// Returns <c>true</c> if the message's QPD-1 carries the prescription-query name.
    /// Lets a dispatcher fan out the same <c>QBP^Q22</c> trigger to either the PDQ
    /// responder or the prescription responder by inspecting QPD-1.
    /// </summary>
    public static bool IsPrescriptionQuery(Hl7V2Message message)
    {
        ArgumentNullException.ThrowIfNull(message);
        // QPD-1 is a CWE: "0^MDC_HDIALY_RX_QUERY^MDC". The semantic name lives in .2.
        var name = message.GetValue("QPD.1.2") ?? message.GetValue("QPD.1.1") ?? message.GetValue("QPD.1");
        return string.Equals(name, QueryName, StringComparison.OrdinalIgnoreCase);
    }

    public static PrescriptionQuery Parse(Hl7V2Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var queryTag = message.GetValue("QPD.2") ?? string.Empty;
        var messageControlId = message.GetValue("MSH.10") ?? string.Empty;

        string? mrn = null;
        for (var repeat = 1; repeat <= 16; repeat++)
        {
            var token = message.GetValue($"QPD.3[{repeat}].1");
            if (string.IsNullOrEmpty(token))
                break;
            var value = message.GetValue($"QPD.3[{repeat}].2");
            var idType = message.GetValue($"QPD.3[{repeat}].6");
            if (string.IsNullOrEmpty(value))
                continue;
            // Per IG §5.2.1: initially only the MRN field is included; idType MR (or empty,
            // since MR is the default for PID.3 in single-domain mode) means MRN.
            if (token.Equals("@PID.3", StringComparison.OrdinalIgnoreCase)
                && (string.IsNullOrEmpty(idType) || idType.Equals("MR", StringComparison.OrdinalIgnoreCase)))
            {
                mrn = value;
            }
        }

        return new PrescriptionQuery(queryTag, messageControlId, mrn);
    }
}
