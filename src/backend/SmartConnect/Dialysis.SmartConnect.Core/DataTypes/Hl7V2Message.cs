using System.Text;
using System.Text.RegularExpressions;

namespace Dialysis.SmartConnect.DataTypes;

/// <summary>
/// In-memory representation of a parsed HL7 v2.x message with field-level addressing.
/// </summary>
public sealed partial class Hl7V2Message : ParsedMessage
{
    private readonly List<Hl7Segment> _segments;
    private readonly char _fieldSep;
    private readonly char _componentSep;
    private readonly char _subComponentSep;
    private readonly char _repeatSep;
    private readonly char _escapeSep;

    private Hl7V2Message(
        List<Hl7Segment> segments,
        char fieldSep,
        char componentSep,
        char subComponentSep,
        char repeatSep,
        char escapeSep)
    {
        _segments = segments;
        _fieldSep = fieldSep;
        _componentSep = componentSep;
        _subComponentSep = subComponentSep;
        _repeatSep = repeatSep;
        _escapeSep = escapeSep;
    }

    public override string DataType => "hl7v2";

    public IReadOnlyList<Hl7Segment> Segments => _segments;

    public static Hl7V2Message Parse(string raw)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raw);

        var lines = raw.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0 || !lines[0].StartsWith("MSH"))
        {
            throw new FormatException("HL7 v2 message must begin with MSH segment.");
        }

        // MSH encoding characters are at positions 3 (field sep) and then 4 chars for comp/rep/esc/sub
        var mshLine = lines[0];
        if (mshLine.Length < 8)
        {
            throw new FormatException("MSH segment is too short to contain encoding characters.");
        }

        var fieldSep = mshLine[3]; // typically '|'
        var componentSep = mshLine[4]; // typically '^'
        var repeatSep = mshLine[5]; // typically '~'
        var escapeSep = mshLine[6]; // typically '\\'
        var subComponentSep = mshLine[7]; // typically '&'

        var segments = new List<Hl7Segment>(lines.Length);
        foreach (var line in lines)
        {
            var fields = line.Split(fieldSep);
            var segName = fields[0];

            // For MSH, field[1] is the encoding chars (special)
            var fieldList = new List<string[][]>();
            var startIndex = segName == "MSH" ? 1 : 1;
            for (var i = startIndex; i < fields.Length; i++)
            {
                // Split repeats
                var repeats = fields[i].Split(repeatSep);
                var repeatComponents = new string[repeats.Length][];
                for (var r = 0; r < repeats.Length; r++)
                {
                    repeatComponents[r] = repeats[r].Split(componentSep);
                }

                fieldList.Add(repeatComponents);
            }

            segments.Add(new Hl7Segment(segName, fieldList));
        }

        return new Hl7V2Message(segments, fieldSep, componentSep, subComponentSep, repeatSep, escapeSep);
    }

    /// <summary>
    /// Get value by path. Syntax: SEGMENT.field[.component[.subcomponent]] with optional repeat [n].
    /// Field numbering: MSH.1 = field separator, MSH.2 = encoding chars, MSH.9 = message type.
    /// For non-MSH segments: PID.3 = 3rd field after segment name.
    /// </summary>
    public override string? GetValue(string path)
    {
        if (!TryParsePath(path, out var segName, out var fieldIndex, out var repeatIndex, out var componentIndex, out var subIndex))
        {
            return null;
        }

        var seg = _segments.FirstOrDefault(s =>
            string.Equals(s.Name, segName, StringComparison.OrdinalIgnoreCase));
        if (seg is null)
        {
            return null;
        }

        // Adjust for MSH special fields (MSH.1 = |, MSH.2 = encoding chars, stored at fieldList[0])
        var adjustedFieldIndex = seg.Name == "MSH" ? fieldIndex - 2 : fieldIndex - 1;
        if (adjustedFieldIndex < 0 || adjustedFieldIndex >= seg.Fields.Count)
        {
            return null;
        }

        var field = seg.Fields[adjustedFieldIndex];
        var ri = repeatIndex - 1;
        if (ri < 0 || ri >= field.Length)
        {
            return null;
        }

        var components = field[ri];
        if (componentIndex == 0)
        {
            // No component specified — return full field value with components joined
            return string.Join(_componentSep, components);
        }

        var ci = componentIndex - 1;
        if (ci < 0 || ci >= components.Length)
        {
            return componentIndex == 1 && components.Length > 0 ? components[0] : null;
        }

        var val = components[ci];

        if (subIndex > 0)
        {
            var subs = val.Split(_subComponentSep);
            var si = subIndex - 1;
            return si < subs.Length ? subs[si] : null;
        }

        return val;
    }

    public override ParsedMessage SetValue(string path, string value)
    {
        if (!TryParsePath(path, out var segName, out var fieldIndex, out var repeatIndex, out var componentIndex, out _))
        {
            return this;
        }

        var seg = _segments.FirstOrDefault(s =>
            string.Equals(s.Name, segName, StringComparison.OrdinalIgnoreCase));
        if (seg is null)
        {
            return this;
        }

        var adjustedFieldIndex = seg.Name == "MSH" ? fieldIndex - 2 : fieldIndex - 1;
        if (adjustedFieldIndex < 0 || adjustedFieldIndex >= seg.Fields.Count)
        {
            return this;
        }

        var field = seg.Fields[adjustedFieldIndex];
        var ri = repeatIndex - 1;
        if (ri < 0 || ri >= field.Length)
        {
            return this;
        }

        var components = field[ri];
        var ci = componentIndex - 1;
        if (ci < 0)
        {
            return this;
        }

        // Extend array if needed
        if (ci >= components.Length)
        {
            var extended = new string[ci + 1];
            Array.Copy(components, extended, components.Length);
            for (var i = components.Length; i < extended.Length; i++)
            {
                extended[i] = "";
            }

            field[ri] = extended;
            components = extended;
        }

        components[ci] = value;
        return this;
    }

    public override string Serialize()
    {
        var sb = new StringBuilder();
        for (var s = 0; s < _segments.Count; s++)
        {
            var seg = _segments[s];
            sb.Append(seg.Name);
            if (seg.Name == "MSH")
            {
                sb.Append(_fieldSep);
                // MSH.1 is the field separator (already appended), MSH.2 is encoding chars
                sb.Append(_componentSep);
                sb.Append(_repeatSep);
                sb.Append(_escapeSep);
                sb.Append(_subComponentSep);
                // Remaining fields start at index 2 (adjustedFieldIndex 1)
                for (var f = 1; f < seg.Fields.Count; f++)
                {
                    sb.Append(_fieldSep);
                    AppendField(sb, seg.Fields[f]);
                }
            }
            else
            {
                for (var f = 0; f < seg.Fields.Count; f++)
                {
                    sb.Append(_fieldSep);
                    AppendField(sb, seg.Fields[f]);
                }
            }

            if (s < _segments.Count - 1)
            {
                sb.Append('\r');
            }
        }

        return sb.ToString();
    }

    private void AppendField(StringBuilder sb, string[][] repeats)
    {
        for (var r = 0; r < repeats.Length; r++)
        {
            if (r > 0)
            {
                sb.Append(_repeatSep);
            }

            var components = repeats[r];
            for (var c = 0; c < components.Length; c++)
            {
                if (c > 0)
                {
                    sb.Append(_componentSep);
                }

                sb.Append(components[c]);
            }
        }
    }

    private static bool TryParsePath(
        string path,
        out string segmentName,
        out int fieldIndex,
        out int repeatIndex,
        out int componentIndex,
        out int subComponentIndex)
    {
        segmentName = "";
        fieldIndex = 0;
        repeatIndex = 1;
        componentIndex = 0;
        subComponentIndex = 0;

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        // Match: SEG.field[repeat].component.subcomponent
        var match = PathRegex().Match(path);
        if (!match.Success)
        {
            return false;
        }

        segmentName = match.Groups["seg"].Value;
        fieldIndex = int.Parse(match.Groups["field"].Value);

        if (match.Groups["rep"].Success)
        {
            repeatIndex = int.Parse(match.Groups["rep"].Value);
        }

        if (match.Groups["comp"].Success)
        {
            componentIndex = int.Parse(match.Groups["comp"].Value);
        }

        if (match.Groups["sub"].Success)
        {
            subComponentIndex = int.Parse(match.Groups["sub"].Value);
        }

        return true;
    }

    [GeneratedRegex(@"^(?<seg>[A-Z][A-Z0-9]{2})\.(?<field>\d+)(?:\[(?<rep>\d+)\])?(?:\.(?<comp>\d+))?(?:\.(?<sub>\d+))?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex PathRegex();
}

/// <summary>One HL7 v2 segment (e.g. MSH, PID, OBR).</summary>
public sealed class Hl7Segment
{
    public Hl7Segment(string name, List<string[][]> fields)
    {
        Name = name;
        Fields = fields;
    }

    public string Name { get; }

    /// <summary>
    /// Fields (0-indexed internally). Each field is an array of repeats; each repeat is an array of components.
    /// </summary>
    public List<string[][]> Fields { get; }
}
