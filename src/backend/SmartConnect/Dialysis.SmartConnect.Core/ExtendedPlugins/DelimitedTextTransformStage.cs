using System.Text;
using System.Text.Json;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>
/// Slice L of the SmartConnect ↔ Mirth alignment plan: a delimited-text parser (CSV / TSV /
/// pipe / any custom delimiter) that emits a JSON array of records. Compose with the
/// existing <see cref="IteratorTransformStage"/> to fan one inbound CSV file into one
/// message per row; left alone it produces a single JSON message carrying all rows.
/// Mirth UG p. 333 ("Delimited Text Data Type") is the reference shape.
/// </summary>
/// <remarks>
/// Parameters JSON (all optional):
/// <code>
/// {
///   "delimiter": ",",       // single-char field separator; defaults to ","
///   "hasHeaderRow": true,    // first row supplies object keys when true (default)
///   "trimWhitespace": true,  // strip leading/trailing whitespace per cell (default true)
///   "skipBlankLines": true   // ignore lines that contain only whitespace (default true)
/// }
/// </code>
/// Output:
/// <list type="bullet">
///   <item><c>hasHeaderRow=true</c> → JSON array of objects keyed by header
///     (<c>[{"PatientId":"MRN-1","Value":"4.5"}, …]</c>).</item>
///   <item><c>hasHeaderRow=false</c> → JSON array of arrays
///     (<c>[["MRN-1","4.5"], …]</c>) so positional pipelines stay simple.</item>
/// </list>
/// </remarks>
public sealed class DelimitedTextTransformStage : ITransformStage
{
    public const string KindValue = "delimited-text";
    public const string ParametersMetadataKey = "smartconnect.transform.parameters";

    public string Kind => KindValue;

    public Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var options = ReadOptions(message);
        var payloadText = Encoding.UTF8.GetString(message.Payload.Span);

        var rows = ParseRows(payloadText, options);
        var json = ProjectRows(rows, options);
        return Task.FromResult(message.CloneWithPayload(Encoding.UTF8.GetBytes(json), PayloadFormat.Utf8Text));
    }

    private static List<string[]> ParseRows(string payload, DelimitedTextOptions options)
    {
        var rows = new List<string[]>();
        foreach (var rawLine in payload.Split(['\r', '\n'], StringSplitOptions.None))
        {
            if (options.SkipBlankLines && string.IsNullOrWhiteSpace(rawLine))
                continue;

            var fields = SplitRespectingQuotes(rawLine, options.Delimiter);
            if (options.TrimWhitespace)
            {
                for (var i = 0; i < fields.Length; i++)
                    fields[i] = fields[i].Trim();
            }
            rows.Add(fields);
        }
        return rows;
    }

    /// <summary>
    /// Minimal RFC 4180 splitter: handles double-quoted fields with embedded delimiters and
    /// escaped quotes (""). Doesn't try to be a full CSV library — the dialysis CSV drops
    /// we've seen are well-formed; if a partner ships exotic CSV, swap this for CsvHelper
    /// in a follow-up.
    /// </summary>
    private static string[] SplitRespectingQuotes(string line, char delimiter)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                    continue;
                }
                if (c == '"')
                {
                    inQuotes = false;
                    continue;
                }
                current.Append(c);
                continue;
            }

            if (c == '"')
            {
                inQuotes = true;
                continue;
            }
            if (c == delimiter)
            {
                fields.Add(current.ToString());
                current.Clear();
                continue;
            }
            current.Append(c);
        }
        fields.Add(current.ToString());
        return [.. fields];
    }

    private static string ProjectRows(List<string[]> rows, DelimitedTextOptions options)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            if (options.HasHeaderRow && rows.Count > 0)
            {
                var header = rows[0];
                writer.WriteStartArray();
                for (var r = 1; r < rows.Count; r++)
                {
                    writer.WriteStartObject();
                    var row = rows[r];
                    for (var c = 0; c < header.Length; c++)
                    {
                        writer.WritePropertyName(header[c]);
                        writer.WriteStringValue(c < row.Length ? row[c] : string.Empty);
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            else
            {
                writer.WriteStartArray();
                foreach (var row in rows)
                {
                    writer.WriteStartArray();
                    foreach (var field in row)
                        writer.WriteStringValue(field);
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();
            }
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static DelimitedTextOptions ReadOptions(IntegrationMessage message)
    {
        if (!message.Metadata.TryGetValue(ParametersMetadataKey, out var parametersJson) ||
            string.IsNullOrWhiteSpace(parametersJson))
            return DelimitedTextOptions.Default;

        try
        {
            using var doc = JsonDocument.Parse(parametersJson);
            var root = doc.RootElement;

            var delimiter = ',';
            if (root.TryGetProperty("delimiter", out var del) && del.ValueKind == JsonValueKind.String)
            {
                var raw = del.GetString();
                if (!string.IsNullOrEmpty(raw))
                    delimiter = ResolveDelimiter(raw);
            }

            var hasHeaderRow = ReadBoolean(root, "hasHeaderRow", defaultValue: true);
            var trimWhitespace = ReadBoolean(root, "trimWhitespace", defaultValue: true);
            var skipBlankLines = ReadBoolean(root, "skipBlankLines", defaultValue: true);

            return new DelimitedTextOptions(delimiter, hasHeaderRow, trimWhitespace, skipBlankLines);
        }
        catch (JsonException)
        {
            return DelimitedTextOptions.Default;
        }
    }

    private static bool ReadBoolean(JsonElement root, string property, bool defaultValue)
    {
        if (!root.TryGetProperty(property, out var prop)) return defaultValue;
        return prop.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => defaultValue,
        };
    }

    /// <summary>
    /// Accept the symbolic delimiter names operators are likely to type into a JSON config —
    /// "\t" / "tab" → '\t', "|" / "pipe" → '|', single-char strings as-is.
    /// </summary>
    private static char ResolveDelimiter(string raw) => raw switch
    {
        "\\t" or "tab" or "TAB" => '\t',
        "pipe" or "PIPE" => '|',
        _ when raw.Length == 1 => raw[0],
        _ => raw[0], // first character — defensive fallback so a stray multi-char value still parses
    };

    private sealed record DelimitedTextOptions(
        char Delimiter,
        bool HasHeaderRow,
        bool TrimWhitespace,
        bool SkipBlankLines)
    {
        public static DelimitedTextOptions Default { get; } =
            new(',', HasHeaderRow: true, TrimWhitespace: true, SkipBlankLines: true);
    }
}
