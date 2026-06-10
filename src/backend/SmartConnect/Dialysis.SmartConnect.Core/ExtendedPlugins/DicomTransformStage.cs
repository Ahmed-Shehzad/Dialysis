using System.Globalization;
using System.Text;
using System.Text.Json;
using FellowOakDicom;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>
/// Slice E of the SmartConnect ↔ Mirth alignment plan: parses an inbound DICOM payload
/// into a JSON-addressable form so downstream JSON-path / mapper / JavaScript transforms
/// can read tag values without writing custom parsers. Mirth UG pp. 324–335 names DICOM
/// as a first-class data type; SmartConnect now has the parser side here, and the
/// attachment side via <see cref="Attachments.Handlers.DicomAttachmentHandler"/> (pixel
/// data still flows through there to avoid bloating the ledger with imaging bytes).
/// </summary>
/// <remarks>
/// Parameters JSON shape (all optional):
/// <code>
/// {
///   "includeTags": ["00100010", "00100020"],      // restrict output to these tags (hex group/element)
///   "includePixelData": false,                     // default false — pixel data is huge and belongs in attachments
///   "format": "by-name"                            // "by-name" (default) or "by-tag" (hex GGGG,EEEE keys)
/// }
/// </code>
/// </remarks>
public sealed class DicomTransformStage : ITransformStage
{
    public const string KindValue = "dicom";
    public const string ParametersMetadataKey = "smartconnect.transform.parameters";

    public string Kind => KindValue;

    public async Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var options = ReadOptions(message);

        DicomFile dicom;
        try
        {
            using var input = new MemoryStream(message.Payload.ToArray());
            dicom = await DicomFile.OpenAsync(input).ConfigureAwait(false);
        }
        catch (DicomFileException)
        {
            return message;
        }
        catch (DicomDataException)
        {
            return message;
        }

        var json = ProjectDataset(dicom.Dataset, options);
        return message.CloneWithPayload(Encoding.UTF8.GetBytes(json), PayloadFormat.Utf8Text);
    }

    private static string ProjectDataset(DicomDataset dataset, DicomTransformOptions options)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteDatasetObject(writer, dataset, options);
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Slice E2: writes a DICOM dataset as a JSON object, recursing into <see cref="DicomSequence"/>
    /// items so SQ-VR elements project as nested arrays of objects (one per sequence item) rather
    /// than getting silently skipped. The <c>includeTags</c> filter applies at the top level only —
    /// once an SQ is included, every element inside its items is emitted so JSONPath expressions
    /// like <c>$.RequestAttributesSequence[0].RequestedProcedureID</c> work end-to-end.
    /// </summary>
    private static void WriteDatasetObject(Utf8JsonWriter writer, DicomDataset dataset, DicomTransformOptions options)
    {
        writer.WriteStartObject();
        foreach (var item in dataset)
        {
            if (item.Tag == DicomTag.PixelData && !options.IncludePixelData)
                continue;
            if (options.IncludeTags is { Count: > 0 } && !options.IncludeTags.Contains(item.Tag))
                continue;

            var key = ResolveKey(item.Tag, options);
            writer.WritePropertyName(key);

            switch (item)
            {
                case DicomSequence sequence:
                    writer.WriteStartArray();
                    // Sequence items are themselves DicomDatasets; project each recursively.
                    // Honour an <c>includeTags</c> filter that opted into the sequence by NOT
                    // re-applying the same filter inside (otherwise an operator who passes only
                    // the outer SQ tag would get back empty objects).
                    var nestedOptions = options.IncludeTags is { Count: > 0 }
                        ? options with { IncludeTags = null }
                        : options;
                    foreach (var nested in sequence.Items)
                    {
                        WriteDatasetObject(writer, nested, nestedOptions);
                    }
                    writer.WriteEndArray();
                    break;
                case DicomElement element:
                    WriteValue(writer, element);
                    break;
                default:
                    // DicomFragmentSequence (encapsulated pixel data) and any other future
                    // item types — write null so the key still appears but downstream knows
                    // nothing was extracted.
                    writer.WriteNullValue();
                    break;
            }
        }
        writer.WriteEndObject();
    }

    private static string ResolveKey(DicomTag tag, DicomTransformOptions options) =>
        options.Format == DicomKeyFormat.ByTag
            ? FormatTagKey(tag)
            : (tag.DictionaryEntry.Keyword is { Length: > 0 } kw ? kw : FormatTagKey(tag));

    private static void WriteValue(Utf8JsonWriter writer, DicomElement element)
    {
        // Multi-valued elements (Count > 1) are emitted as JSON arrays so JSONPath
        // expressions like $.OtherPatientIDs[1] work without bespoke string splitting.
        if (element.Count > 1)
        {
            writer.WriteStartArray();
            for (var i = 0; i < element.Count; i++)
            {
                writer.WriteStringValue(element.Get<string>(i) ?? string.Empty);
            }
            writer.WriteEndArray();
            return;
        }

        if (element.Count == 0)
        {
            writer.WriteNullValue();
            return;
        }

        var raw = element.Get<string>(0) ?? string.Empty;
        writer.WriteStringValue(raw);
    }

    private static string FormatTagKey(DicomTag tag) =>
        $"{tag.Group:X4}{tag.Element:X4}";

    private static DicomTransformOptions ReadOptions(IntegrationMessage message)
    {
        if (!message.Metadata.TryGetValue(ParametersMetadataKey, out var parametersJson) ||
            string.IsNullOrWhiteSpace(parametersJson))
            return DicomTransformOptions.Default;

        try
        {
            using var doc = JsonDocument.Parse(parametersJson);
            var root = doc.RootElement;

            var format = DicomKeyFormat.ByName;
            if (root.TryGetProperty("format", out var fmt) && fmt.ValueKind == JsonValueKind.String &&
                string.Equals(fmt.GetString(), "by-tag", StringComparison.OrdinalIgnoreCase))
            {
                format = DicomKeyFormat.ByTag;
            }

            var includePixelData = false;
            if (root.TryGetProperty("includePixelData", out var pix) && pix.ValueKind == JsonValueKind.True)
                includePixelData = true;

            HashSet<DicomTag>? includeTags = null;
            if (root.TryGetProperty("includeTags", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                includeTags = [];
                foreach (var el in arr.EnumerateArray())
                {
                    if (el.ValueKind == JsonValueKind.String && TryParseTag(el.GetString(), out var tag))
                        includeTags.Add(tag);
                }
            }

            return new DicomTransformOptions(format, includePixelData, includeTags);
        }
        catch (JsonException)
        {
            return DicomTransformOptions.Default;
        }
    }

    private static bool TryParseTag(string? raw, out DicomTag tag)
    {
        tag = null!;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        var trimmed = raw.Replace("(", string.Empty, StringComparison.Ordinal)
                         .Replace(")", string.Empty, StringComparison.Ordinal)
                         .Replace(",", string.Empty, StringComparison.Ordinal);
        if (trimmed.Length != 8)
            return false;
        if (!ushort.TryParse(trimmed[..4], NumberStyles.HexNumber,
                CultureInfo.InvariantCulture, out var group))
            return false;
        if (!ushort.TryParse(trimmed[4..], NumberStyles.HexNumber,
                CultureInfo.InvariantCulture, out var element))
            return false;
        tag = new DicomTag(group, element);
        return true;
    }

    private sealed record DicomTransformOptions
    {
        public DicomTransformOptions(DicomKeyFormat Format,
            bool IncludePixelData,
            HashSet<DicomTag>? IncludeTags)
        {
            this.Format = Format;
            this.IncludePixelData = IncludePixelData;
            this.IncludeTags = IncludeTags;
        }

        public static DicomTransformOptions Default { get; } = new(DicomKeyFormat.ByName, IncludePixelData: false, IncludeTags: null);

        public DicomKeyFormat Format { get; init; }
        public bool IncludePixelData { get; init; }
        public HashSet<DicomTag>? IncludeTags { get; init; }
        public void Deconstruct(out DicomKeyFormat Format, out bool IncludePixelData, out HashSet<DicomTag>? IncludeTags)
        {
            Format = this.Format;
            IncludePixelData = this.IncludePixelData;
            IncludeTags = this.IncludeTags;
        }
    }

    private enum DicomKeyFormat
    {
        ByName = 0,
        ByTag = 1,
    }
}
