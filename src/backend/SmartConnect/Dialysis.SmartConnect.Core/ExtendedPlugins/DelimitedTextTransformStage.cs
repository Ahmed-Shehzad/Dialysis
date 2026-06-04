using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.DataTypes;

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
    /// Pull the first non-blank row off the reader to serve as the header. Delegates to the
    /// shared <see cref="DelimitedTextStreaming"/> helper so this stage and the File Reader
    /// (slice D2) parse via one code path.
    /// </summary>
    private static string[]? ReadFirstNonBlankRow(StreamReader reader, DelimitedTextOptions options)
        => DelimitedTextStreaming.ReadHeader(reader, options.ToHelperOptions());

    /// <summary>
    /// Lazy row iterator delegated to the shared helper. See
    /// <see cref="DelimitedTextStreaming.EnumerateRecords"/> for the streaming guarantees.
    /// </summary>
    private static IEnumerable<string[]> EnumerateRows(StreamReader reader, DelimitedTextOptions options)
        => DelimitedTextStreaming.EnumerateRecords(reader, options.ToHelperOptions());

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
                    delimiter = DelimitedTextStreaming.ResolveDelimiter(raw);
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

    private sealed record DelimitedTextOptions
    {
        public DelimitedTextOptions(char Delimiter,
            bool HasHeaderRow,
            bool TrimWhitespace,
            bool SkipBlankLines,
            DelimitedTextOutputFormat OutputFormat)
        {
            this.Delimiter = Delimiter;
            this.HasHeaderRow = HasHeaderRow;
            this.TrimWhitespace = TrimWhitespace;
            this.SkipBlankLines = SkipBlankLines;
            this.OutputFormat = OutputFormat;
        }

        public static DelimitedTextOptions Default { get; } = new(',', HasHeaderRow: true, TrimWhitespace: true, SkipBlankLines: true,
            OutputFormat: DelimitedTextOutputFormat.Array);

        public char Delimiter { get; init; }
        public bool HasHeaderRow { get; init; }
        public bool TrimWhitespace { get; init; }
        public bool SkipBlankLines { get; init; }
        public DelimitedTextOutputFormat OutputFormat { get; init; }

        /// <summary>Project onto the shared streaming helper's option type (no
        /// <see cref="OutputFormat"/>: that's a transform-stage concern).</summary>
        public DelimitedTextStreaming.Options ToHelperOptions() =>
            new(Delimiter, HasHeaderRow, TrimWhitespace, SkipBlankLines);
        public void Deconstruct(out char Delimiter, out bool HasHeaderRow, out bool TrimWhitespace, out bool SkipBlankLines, out DelimitedTextOutputFormat OutputFormat)
        {
            Delimiter = this.Delimiter;
            HasHeaderRow = this.HasHeaderRow;
            TrimWhitespace = this.TrimWhitespace;
            SkipBlankLines = this.SkipBlankLines;
            OutputFormat = this.OutputFormat;
        }
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
