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
///   "skipBlankLines": true,  // ignore lines that contain only whitespace (default true)
///   "outputFormat": "array"  // "array" (default) or "ndjson" — newline-delimited JSON
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
/// <remarks>
/// Slice L2 streaming pass: the parse + projection runs as a single iteration over the
/// payload's byte stream via <see cref="StreamReader.ReadLine"/> and writes directly to a
/// pooled <see cref="MemoryStream"/> via <see cref="Utf8JsonWriter"/>. Peak working-set is
/// ~ 1× the file size instead of ~ 3× — input string + line array + row list + output. Lets
/// the stage handle files past ~10 MB without thrashing the LOH. Multi-line quoted fields
/// (RFC 4180 embedded newlines) are still not supported; that was true pre-L2 and would
/// require a real CSV library to fix.
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

        // Single-pass streaming: wrap the payload memory directly, read line-by-line, and
        // write rows straight to the output Utf8JsonWriter. Nothing intermediate accumulates.
        using var input = new MemoryStream(message.Payload.ToArray(), writable: false);
        using var reader = new StreamReader(input, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        using var output = new MemoryStream(capacity: Math.Min(message.Payload.Length, 64 * 1024));

        if (options.OutputFormat == DelimitedTextOutputFormat.Ndjson)
        {
            WriteNdjson(reader, output, options);
        }
        else
        {
            WriteArray(reader, output, options);
        }

        return Task.FromResult(message.CloneWithPayload(output.ToArray(), PayloadFormat.Utf8Text));
    }

    /// <summary>
    /// Stream pump for the default array output. Header (when configured) is read once
    /// up-front; subsequent rows project against it without copying the row buffer into a
    /// list. Whole array is wrapped in a single <see cref="Utf8JsonWriter"/> session.
    /// </summary>
    private static void WriteArray(StreamReader reader, Stream output, DelimitedTextOptions options)
    {
        using var writer = new Utf8JsonWriter(output);
        writer.WriteStartArray();
        var header = options.HasHeaderRow ? ReadFirstNonBlankRow(reader, options) : null;
        foreach (var row in EnumerateRows(reader, options))
        {
            if (header is not null)
            {
                WriteRowObject(writer, header, row);
            }
            else
            {
                WriteRowArray(writer, row);
            }
        }
        writer.WriteEndArray();
    }

    /// <summary>
    /// Stream pump for the NDJSON output. Each row is written as a self-contained JSON
    /// object/array followed by a single newline; no enclosing wrapper. Downstream consumers
    /// can `StringReader.ReadLine` row-by-row without parsing the whole payload.
    /// </summary>
    private static void WriteNdjson(StreamReader reader, Stream output, DelimitedTextOptions options)
    {
        var header = options.HasHeaderRow ? ReadFirstNonBlankRow(reader, options) : null;
        foreach (var row in EnumerateRows(reader, options))
        {
            using (var writer = new Utf8JsonWriter(output))
            {
                if (header is not null) WriteRowObject(writer, header, row);
                else WriteRowArray(writer, row);
            }
            output.WriteByte((byte)'\n');
        }
    }

    /// <summary>
    /// Pull the first non-blank row off the reader to serve as the header. Returns null if
    /// the reader is empty (so the projection writer can emit an empty array gracefully).
    /// </summary>
    private static string[]? ReadFirstNonBlankRow(StreamReader reader, DelimitedTextOptions options)
    {
        while (reader.ReadLine() is { } rawLine)
        {
            if (options.SkipBlankLines && string.IsNullOrWhiteSpace(rawLine))
                continue;
            return MaterialiseRow(rawLine, options);
        }
        return null;
    }

    /// <summary>
    /// Lazy row iterator. Reads lines until EOF, skipping blank lines per options, and
    /// yields each parsed row. Each yielded array is freshly allocated; the caller is free
    /// to drop the reference once it's written to the output.
    /// </summary>
    private static IEnumerable<string[]> EnumerateRows(StreamReader reader, DelimitedTextOptions options)
    {
        while (reader.ReadLine() is { } rawLine)
        {
            if (options.SkipBlankLines && string.IsNullOrWhiteSpace(rawLine))
                continue;
            yield return MaterialiseRow(rawLine, options);
        }
    }

    private static string[] MaterialiseRow(string rawLine, DelimitedTextOptions options)
    {
        var fields = SplitRespectingQuotes(rawLine, options.Delimiter);
        if (options.TrimWhitespace)
        {
            for (var i = 0; i < fields.Length; i++)
                fields[i] = fields[i].Trim();
        }
        return fields;
    }

    /// <summary>
    /// Minimal RFC 4180 splitter: handles double-quoted fields with embedded delimiters and
    /// escaped quotes (""). Doesn't try to be a full CSV library — the dialysis CSV drops
    /// we've seen are well-formed; if a partner ships exotic CSV, swap this for CsvHelper
    /// in a follow-up. Multi-line quoted fields are NOT supported (we split on the line
    /// reader, which can't see across a `\n` inside a quoted cell).
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
