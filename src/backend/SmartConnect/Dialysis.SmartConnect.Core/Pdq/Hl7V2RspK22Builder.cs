using System.Globalization;
using System.Text;

namespace Dialysis.SmartConnect.Pdq;

/// <summary>
/// Builds an IHE PDQ <c>RSP^K22^RSP_K21</c> response frame from a parsed
/// <see cref="PdqCriteria"/> and the matches returned by
/// <see cref="IPatientDemographicsResolver"/>.
/// </summary>
/// <remarks>
/// Wire layout (per IG Section 4.2.2 + samples in Section 4.3):
/// <list type="bullet">
///   <item>MSH — receiving fields empty (single-domain demographics supplier per IG note),
///         MSH-9 fixed to <c>RSP^K22^RSP_K21</c>, MSH-10 set to a fresh control id, MSH-15/16
///         = <c>NE</c>/<c>NE</c> (per Example 2).</item>
///   <item>MSA — <c>AA</c>, MSA-2 echoes the inbound MSH-10.</item>
///   <item>QAK — QueryTag echoed, response status <c>OK</c> when matches exist else
///         <c>NF</c>, query name <c>IHE PDQ Query</c>, hits count.</item>
///   <item>QPD — echoes the inbound criteria token so the machine can verify QAK-3 / QPD-1
///         match.</item>
///   <item>PID — one per match. Populates PID-3 (MRN with id-type MR), PID-5 (family/given,
///         name-type-code U for unspecified), and PID-7 (DOB as YYYYMMDD) where available.</item>
/// </list>
/// Default HL7 separators are used (<c>|^~\&amp;</c>). The frame is terminated with <c>\r</c>
/// segments per HL7 v2 convention.
/// </remarks>
public static class Hl7V2RspK22Builder
{
    private const string MessageType = "RSP^K22^RSP_K21";
    private const string ProcessingId = "P";
    private const string VersionId = "2.6";
    private const string QueryName = "IHE PDQ Query";

    public static string Build(
        PdqCriteria inbound,
        IReadOnlyList<PdqMatch> matches,
        string responseControlId,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(inbound);
        ArgumentNullException.ThrowIfNull(matches);
        ArgumentException.ThrowIfNullOrWhiteSpace(responseControlId);

        var hits = matches.Count;
        var status = hits == 0 ? "NF" : "OK";

        var sb = new StringBuilder();
        sb.Append("MSH|^~\\&|||||").Append(FormatTs(nowUtc)).Append("||").Append(MessageType)
            .Append('|').Append(responseControlId).Append('|').Append(ProcessingId).Append('|').Append(VersionId)
            .Append("|||NE|NE|||||\r");

        sb.Append("MSA|AA|").Append(inbound.MessageControlId).Append('\r');

        // QAK-1 query tag (echo), QAK-2 status, QAK-3 query name, QAK-4 hits this fetch,
        // QAK-5 hits remaining, QAK-6 total hits.
        sb.Append("QAK|").Append(inbound.QueryTag).Append('|').Append(status).Append('|')
            .Append(QueryName).Append('|').Append(hits).Append('|').Append(hits).Append("|0\r");

        sb.Append("QPD|").Append(QueryName).Append('|').Append(inbound.QueryTag).Append('|')
            .Append(EchoCriteria(inbound)).Append('\r');

        foreach (var m in matches)
        {
            sb.Append("PID|||")
                .Append(EscapeField(m.MedicalRecordNumber)).Append("^^^^MR||")
                .Append(EscapeField(m.FamilyName)).Append('^').Append(EscapeField(m.GivenName))
                .Append("^^^^^U||")
                .Append(m.DateOfBirth?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(m.SexAtBirthCode))
                sb.Append('|').Append(EscapeField(m.SexAtBirthCode));
            sb.Append('\r');
        }

        return sb.ToString();
    }

    private static string EchoCriteria(PdqCriteria criteria)
    {
        // Repeat-separator = ~. Echo only the tokens we parsed so the response stays
        // round-trippable against the IG examples.
        var parts = new List<string>(4);
        if (!string.IsNullOrWhiteSpace(criteria.MedicalRecordNumber))
            parts.Add($"@PID.3^{EscapeField(criteria.MedicalRecordNumber)}^^^^MR");
        if (!string.IsNullOrWhiteSpace(criteria.PersonNumber))
            parts.Add($"@PID.3^{EscapeField(criteria.PersonNumber)}^^^^PN");
        if (!string.IsNullOrWhiteSpace(criteria.FamilyName))
            parts.Add($"@PID.5.1^{EscapeField(criteria.FamilyName)}");
        if (!string.IsNullOrWhiteSpace(criteria.GivenName))
            parts.Add($"@PID.5.2^{EscapeField(criteria.GivenName)}");
        return string.Join('~', parts);
    }

    private static string FormatTs(DateTime utc) =>
        utc.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);

    private static string EscapeField(string value)
    {
        // Defensive: strip HL7 separators from caller-supplied strings so a name like
        // "Smith|Jones" can't break the wire frame. Full HL7 escape sequences are out of
        // scope here — the upstream resolver should already produce clean text.
        return value
            .Replace("|", string.Empty, StringComparison.Ordinal)
            .Replace("^", string.Empty, StringComparison.Ordinal)
            .Replace("~", string.Empty, StringComparison.Ordinal)
            .Replace("\\", string.Empty, StringComparison.Ordinal)
            .Replace("&", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
    }
}
