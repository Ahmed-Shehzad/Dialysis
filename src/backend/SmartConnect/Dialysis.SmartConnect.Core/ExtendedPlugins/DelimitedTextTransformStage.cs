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

    private static string ProjectRows(List<string[]> rows, DelimitedTextOptions options) =>
        options.OutputFormat == DelimitedTextOutputFormat.Ndjson
            ? ProjectNdjson(rows, options)
            : ProjectArray(rows, options);

    /// <summary>
    /// Default array output — backward compatible. The whole row set materialises into a
    /// single JSON array. Fine for partner files we've seen (≲ 10 MB).
    /// </summary>
    private static string ProjectArray(List<string[]> rows, DelimitedTextOptions options)
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
                    WriteRowObject(writer, header, rows[r]);
                }
                writer.WriteEndArray();
            }
            else
            {
                writer.WriteStartArray();
                foreach (var row in rows)
                {
                    WriteRowArray(writer, row);
                }
                writer.WriteEndArray();
            }
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Slice L2 streaming-friendly output: newline-delimited JSON (NDJSON / JSON Lines).
    /// One JSON object per row, each terminated by <c>\n</c>; no enclosing array. Downstream
    /// transforms can stream the payload line-by-line via <c>StringReader.ReadLine</c>
    /// without materialising the full array — useful when partner files exceed ~10 MB.
    /// </summary>
    private static string ProjectNdjson(List<string[]> rows, DelimitedTextOptions options)
    {
        var sb = new StringBuilder();
        if (options.HasHeaderRow && rows.Count > 0)
        {
            var header = rows[0];
            for (var r = 1; r < rows.Count; r++)
            {
                AppendRowJson(sb, header, rows[r]);
                sb.Append('\n');
            }
        }
        else
        {
            foreach (var row in rows)
            {
                AppendRowArrayJson(sb, row);
                sb.Append('\n');
            }
        }
        return sb.ToString();
    }

    private static void WriteRowObject(Utf8JsonWriter writer, string[] header, string[] row)
    {
        writer.WriteStartObject();
        for (var c = 0; c < header.Length; c++)
        {
            writer.WritePropertyName(header[c]);
            writer.WriteStringValue(c < row.Length ? row[c] : string.Empty);
        }
        writer.WriteEndObject();
    }

    private static void WriteRowArray(Utf8JsonWriter writer, string[] row)
    {
        writer.WriteStartArray();
        foreach (var field in row)
            writer.WriteStringValue(field);
        writer.WriteEndArray();
    }

    private static void AppendRowJson(StringBuilder sb, string[] header, string[] row)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteRowObject(writer, header, row);
        }
        sb.Append(Encoding.UTF8.GetString(stream.ToArray()));
    }

    private static void AppendRowArrayJson(StringBuilder sb, string[] row)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteRowArray(writer, row);
        }
        sb.Append(Encoding.UTF8.GetString(stream.ToArray()));
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

            // Slice L2: optional NDJSON output for large-file streaming. Default stays
            // "array" so existing flows aren't disrupted.
            var outputFormat = DelimitedTextOutputFormat.Array;
            if (root.TryGetProperty("outputFormat", out var fmt) && fmt.ValueKind == JsonValueKind.String &&
                string.Equals(fmt.GetString(), "ndjson", StringComparison.OrdinalIgnoreCase))
            {
                outputFormat = DelimitedTextOutputFormat.Ndjson;
            }

            return new DelimitedTextOptions(delimiter, hasHeaderRow, trimWhitespace, skipBlankLines, outputFormat);
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
        bool SkipBlankLines,
        DelimitedTextOutputFormat OutputFormat)
    {
        public static DelimitedTextOptions Default { get; } =
            new(',', HasHeaderRow: true, TrimWhitespace: true, SkipBlankLines: true,
                OutputFormat: DelimitedTextOutputFormat.Array);
    }

    private enum DelimitedTextOutputFormat
    {
        /// <summary>Single JSON array enclosing every record (backward-compatible default).</summary>
        Array = 0,

        /// <summary>Newline-delimited JSON — one object per line, no enclosing array. Slice L2
        /// composition pattern for large files: downstream can stream the payload via
        /// <c>StringReader.ReadLine</c>.</summary>
        Ndjson = 1,
    }
}
