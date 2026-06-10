using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.XPath;

namespace Dialysis.SmartConnect.Iteration;

/// <summary>One element produced by <see cref="IterableResolver"/>.</summary>
public readonly record struct IterableElement
{
    /// <summary>One element produced by <see cref="IterableResolver"/>.</summary>
    public IterableElement(int Index, string Value)
    {
        this.Index = Index;
        this.Value = Value;
    }
    public int Index { get; init; }
    public string Value { get; init; }
    public void Deconstruct(out int Index, out string Value)
    {
        Index = this.Index;
        Value = this.Value;
    }
}

/// <summary>
/// Evaluates an iterable expression against an <see cref="IntegrationMessage"/> payload and yields
/// <see cref="IterableElement"/> pairs (index, value). Used by Iterator filter / transform plugins
/// and by the Destination Set Filter when computed routing depends on a per-element check.
///
/// <para>Expression syntax (format auto-detected from payload):</para>
/// <list type="bullet">
///   <item><b>HL7 v2</b>: <c>OBX</c>, <c>*OBX</c>, or <c>OBX[*]</c> iterate every OBX segment. <c>PID.3</c>,
///         <c>PID.3[*]</c>, or <c>PID-3-*</c> iterate every repeat of field 3 in the first PID segment.</item>
///   <item><b>JSON</b>: <c>$.observations[*]</c> or <c>observations[*]</c> iterate every element of the array
///         at the given dot-path. Leading <c>$.</c> optional.</item>
///   <item><b>XML / plain</b>: any XPath nodeset expression — yields the inner text of each matched node.</item>
/// </list>
/// </summary>
public static class IterableResolver
{
    public static IReadOnlyList<IterableElement> Resolve(IntegrationMessage message, string expression)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);

        var payloadText = Encoding.UTF8.GetString(message.Payload.Span);
        var trimmedPayload = payloadText.TrimStart();

        if (trimmedPayload.StartsWith("MSH", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveHl7(payloadText, expression);
        }

        if (message.PayloadFormat == PayloadFormat.Json ||
            (trimmedPayload.Length > 0 && (trimmedPayload[0] == '{' || trimmedPayload[0] == '[')))
        {
            return ResolveJson(payloadText, expression);
        }

        return ResolveXml(payloadText, expression);
    }

    private static IReadOnlyList<IterableElement> ResolveHl7(string payload, string expression)
    {
        // Detect separators from MSH (defaults if not found).
        var (fieldSep, _, repeatSep, _) = ReadHl7Separators(payload);

        // Split into segments — HL7 uses CR; tolerate LF / CRLF as well.
        var segments = payload
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .ToArray();

        var (segName, fieldIndex, wantFieldRepeats) = ParseHl7Expression(expression);

        if (!wantFieldRepeats)
        {
            // Iterate over every segment with the given name.
            var results = new List<IterableElement>();
            var idx = 0;
            foreach (var seg in segments)
            {
                if (seg.StartsWith(segName + fieldSep, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(seg, segName, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new IterableElement(idx++, seg));
                }
            }

            return results;
        }

        // Iterate over the repeats of a specific field in the first matching segment.
        var match = Array.Find(segments, s =>
            s.StartsWith(segName + fieldSep, StringComparison.OrdinalIgnoreCase));
        if (match is null)
            return [];

        var fields = match.Split(fieldSep);
        // MSH has special handling — MSH.1 is the field separator itself, MSH.2 encoding chars; the visible fields
        // start at index 2 (i.e. MSH.3 == fields[2]). For all other segments fields[0] is the segment name itself,
        // so SEG.N == fields[N].
        var fieldArrayIndex = string.Equals(segName, "MSH", StringComparison.OrdinalIgnoreCase)
            ? fieldIndex - 1
            : fieldIndex;
        if (fieldArrayIndex < 0 || fieldArrayIndex >= fields.Length)
            return [];

        var repeats = fields[fieldArrayIndex].Split(repeatSep);
        var output = new IterableElement[repeats.Length];
        for (var i = 0; i < repeats.Length; i++)
            output[i] = new IterableElement(i, repeats[i]);

        return output;
    }

    private static IReadOnlyList<IterableElement> ResolveJson(string payload, string expression)
    {
        var node = JsonNode.Parse(payload);
        if (node is null)
            return [];

        var path = expression.TrimStart();
        if (path.StartsWith('$'))
            path = path[1..];
        if (path.StartsWith('.'))
            path = path[1..];
        if (path.EndsWith("[*]", StringComparison.Ordinal))
            path = path[..^3];

        var current = node;
        if (!string.IsNullOrEmpty(path))
        {
            foreach (var seg in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                if (current is null)
                    return [];
                if (current is JsonObject obj && obj.TryGetPropertyValue(seg, out var child))
                {
                    current = child;
                }
                else
                {
                    return [];
                }
            }
        }

        if (current is not JsonArray array)
            return [];

        var results = new IterableElement[array.Count];
        for (var i = 0; i < array.Count; i++)
            results[i] = new IterableElement(i, array[i]?.ToJsonString() ?? "null");
        return results;
    }

    private static IReadOnlyList<IterableElement> ResolveXml(string payload, string expression)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return [];

        XPathDocument doc;
        try
        {
            using var reader = XmlReader.Create(new StringReader(payload),
                new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
            doc = new XPathDocument(reader);
        }
        catch (XmlException)
        {
            return [];
        }

        var navigator = doc.CreateNavigator();
        XPathNodeIterator iter;
        try
        {
            iter = navigator.Select(expression);
        }
        catch (XPathException)
        {
            return [];
        }

        var results = new List<IterableElement>();
        var idx = 0;
        while (iter.MoveNext())
        {
            results.Add(new IterableElement(idx++, iter.Current?.OuterXml ?? string.Empty));
        }

        return results;
    }

    private static (char Field, char Component, char Repeat, char SubComponent) ReadHl7Separators(string payload)
    {
        // HL7 standard: MSH|^~\& — MSH.1 = '|' (field), encoding chars at MSH.2 = "^~\&"
        // We accept any first character as the field separator (rare to differ).
        if (payload.Length < 8 || !payload.StartsWith("MSH", StringComparison.OrdinalIgnoreCase))
            return ('|', '^', '~', '&');

        var fieldSep = payload[3];
        var component = payload.Length > 4 ? payload[4] : '^';
        var repeat = payload.Length > 5 ? payload[5] : '~';
        var sub = payload.Length > 7 ? payload[7] : '&';
        return (fieldSep, component, repeat, sub);
    }

    private static (string Segment, int FieldIndex, bool WantFieldRepeats) ParseHl7Expression(string expression)
    {
        var expr = expression.Trim();

        // Strip leading '*' for "*OBX" syntax.
        if (expr.StartsWith('*'))
            return (expr[1..].TrimEnd('[', '*', ']'), 0, false);

        // "OBX[*]" — all segments of name OBX
        if (expr.EndsWith("[*]", StringComparison.Ordinal) && !expr.Contains('.') && !expr.Contains('-'))
            return (expr[..^3], 0, false);

        // "PID-3-*" — Mirth-style: convert to "PID.3[*]"
        if (expr.Contains('-'))
        {
            var parts = expr.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx))
            {
                return (parts[0], idx, true);
            }
        }

        // "SEG.N" or "SEG.N[*]" — field repeats
        var dotIdx = expr.IndexOf('.');
        if (dotIdx > 0)
        {
            var segName = expr[..dotIdx];
            var rest = expr[(dotIdx + 1)..].TrimEnd('[', '*', ']');
            if (int.TryParse(rest, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx))
                return (segName, idx, true);
        }

        // Bare segment name → all segments of that name.
        return (expr, 0, false);
    }
}
