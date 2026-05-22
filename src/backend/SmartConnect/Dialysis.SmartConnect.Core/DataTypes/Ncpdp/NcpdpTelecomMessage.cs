namespace Dialysis.SmartConnect.DataTypes.Ncpdp;

/// <summary>
/// Parsed NCPDP Telecom Standard message (5.1 / D.0 / D.1). The standard uses three ASCII
/// control characters as separators: FS (0x1C) between fields, GS (0x1D) between repeating
/// groups within a segment, SS (0x1E) between segments. Each field starts with a 2-character
/// identifier (alphanumeric) followed by the field data; segments after the transaction
/// header begin with an <c>AM</c>-prefixed segment identifier field.
/// </summary>
/// <remarks>
/// SmartConnect's role here is the same as for HL7v2 + DICOM: provide a typed parse tree
/// + JSON projection so downstream JSONPath / mapper / JavaScript transforms can route
/// claims, eligibility verifications, and reversals by transaction code / NDC / patient
/// without writing custom byte-level parsers. Per-transaction FHIR mapping (Claim /
/// MedicationRequest) lands in a follow-up slice K2 once we know which transactions our
/// partner pharmacies actually send.
/// </remarks>
public sealed class NcpdpTelecomMessage
{
    /// <summary>Field separator (FS, ASCII 0x1C).</summary>
    public const char FieldSeparator = '\x1C';

    /// <summary>Group separator (GS, ASCII 0x1D).</summary>
    public const char GroupSeparator = '\x1D';

    /// <summary>Segment separator (SS, ASCII 0x1E).</summary>
    public const char SegmentSeparator = '\x1E';

    private NcpdpTelecomMessage(IReadOnlyList<NcpdpSegment> segments)
    {
        Segments = segments;
        var header = segments.Count > 0 ? segments[0] : null;
        VersionRelease = header?.GetField("A2");
        TransactionCode = header?.GetField("A3");
    }

    public IReadOnlyList<NcpdpSegment> Segments { get; }

    /// <summary>NCPDP Version/Release (field <c>A2</c> on the transaction header) — e.g.
    /// <c>D0</c> for v.D.0, <c>51</c> for v.5.1. <c>null</c> when the message is too
    /// short to carry a header.</summary>
    public string? VersionRelease { get; }

    /// <summary>NCPDP Transaction Code (field <c>A3</c> on the transaction header) — e.g.
    /// <c>B1</c> billing, <c>B2</c> reversal, <c>E1</c> eligibility verification.
    /// <c>null</c> when the message is too short to carry a header.</summary>
    public string? TransactionCode { get; }

    /// <summary>
    /// Parses a UTF-8 / ASCII NCPDP Telecom payload. Returns <c>null</c> when the input
    /// contains no segment-separator characters at all — that's the cleanest signal that
    /// the upstream sent a non-Telecom payload to a Telecom-configured route.
    /// </summary>
    public static NcpdpTelecomMessage? TryParse(string raw)
    {
        if (string.IsNullOrEmpty(raw) || raw.IndexOf(SegmentSeparator, StringComparison.Ordinal) < 0)
            return null;

        var segments = new List<NcpdpSegment>();
        var segmentTexts = raw.Split(SegmentSeparator, StringSplitOptions.RemoveEmptyEntries);
        for (var s = 0; s < segmentTexts.Length; s++)
        {
            var text = segmentTexts[s];
            if (string.IsNullOrEmpty(text))
                continue;
            segments.Add(NcpdpSegment.Parse(text, segmentIndex: s));
        }

        return new NcpdpTelecomMessage(segments);
    }
}

/// <summary>One NCPDP Telecom segment — the transaction header (index 0) or any of the
/// downstream patient / insurance / claim / pharmacy / prescriber segments.</summary>
public sealed class NcpdpSegment
{
    private readonly Dictionary<string, string> _fields;

    private NcpdpSegment(int index, string? segmentId, Dictionary<string, string> fields)
    {
        Index = index;
        SegmentId = segmentId;
        _fields = fields;
    }

    /// <summary>0-based position of this segment in the message.</summary>
    public int Index { get; }

    /// <summary>The <c>AM</c>-prefixed segment identifier (e.g. <c>AM01</c> patient,
    /// <c>AM02</c> pharmacy provider, <c>AM03</c> prescriber). <c>null</c> for the
    /// transaction header at index 0, which carries no <c>AM</c> field.</summary>
    public string? SegmentId { get; }

    /// <summary>Read-only field map keyed by the 2-character NCPDP field ID.</summary>
    public IReadOnlyDictionary<string, string> Fields => _fields;

    public string? GetField(string fieldId) =>
        _fields.TryGetValue(fieldId, out var value) ? value : null;

    internal static NcpdpSegment Parse(string text, int segmentIndex)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? segmentId = null;

        // Field separator is the per-field boundary. Group separators inside a single
        // field signal repeated occurrences — preserve them in the captured value so
        // downstream transforms can split if they care; this minimal parser doesn't
        // expand repeats into structured arrays (that requires the NCPDP data
        // dictionary's per-field cardinality metadata).
        var fieldTexts = text.Split(NcpdpTelecomMessage.FieldSeparator);
        foreach (var raw in fieldTexts)
        {
            if (raw.Length < 2)
                continue;
            var id = raw[..2];
            var value = raw[2..];
            // Multiple instances of the same field id within one segment shouldn't
            // happen in well-formed NCPDP; if they do, last-write-wins is the safest
            // default and the downstream operator dashboard can flag the duplicate.
            fields[id] = value;
            if (id.Equals("AM", StringComparison.OrdinalIgnoreCase))
                segmentId = "AM" + value;
        }

        return new NcpdpSegment(segmentIndex, segmentId, fields);
    }
}
