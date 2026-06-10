using System.Globalization;
using System.Text;
using System.Text.Json;
using FellowOakDicom;

namespace Dialysis.SmartConnect.Attachments.Handlers;

/// <summary>
/// DICOM-aware handler: parses the inbound payload as a DICOM dataset, extracts configured tags (default
/// PixelData <c>(7FE0,0010)</c>) into separate attachments, and replaces them in-dataset with the inline
/// reference token written into a private text element. Mirth UG p224 "DICOM Attachment Handler".
/// Properties JSON: <c>{ "extractTags": ["7FE0,0010", "0042,0011"], "mimeType": "application/dicom" }</c>.
/// </summary>
public sealed class DicomAttachmentHandler : IAttachmentHandler
{
    public const string KindValue = "dicom";

    private static readonly DicomTag _defaultPixelData = DicomTag.PixelData;

    public string Kind => KindValue;

    public async Task<AttachmentHandlerResult> ExtractAsync(
        IntegrationMessage message,
        AttachmentHandlerContext context,
        CancellationToken cancellationToken)
    {
        var (tags, mime) = ParseProperties(context);

        DicomFile dicom;
        try
        {
            using var input = new MemoryStream(message.Payload.ToArray());
            dicom = await DicomFile.OpenAsync(input);
        }
        catch (DicomFileException)
        {
            return AttachmentHandlerResult.Unchanged(message.Payload);
        }
        catch (DicomDataException)
        {
            return AttachmentHandlerResult.Unchanged(message.Payload);
        }

        var attachments = new List<Attachment>(tags.Count);
        foreach (var tag in tags)
        {
            if (!dicom.Dataset.Contains(tag))
                continue;
            var bytes = TryGetBytes(dicom.Dataset, tag);
            if (bytes is null || bytes.Length == 0)
                continue;

            var id = Guid.CreateVersion7();
            attachments.Add(new Attachment
            {
                Id = id,
                MessageId = context.MessageId,
                FlowId = context.FlowId,
                MimeType = mime,
                Data = bytes,
                SizeBytes = bytes.Length,
                CreatedUtc = DateTimeOffset.UtcNow,
            });

            // Replace the element with a textual reference (LongText, fits in any UT/LT slot when re-imported).
            dicom.Dataset.Remove(tag);
            dicom.Dataset.Add(new DicomLongText(tag, AttachmentReference.Format(id)));
        }

        if (attachments.Count == 0)
        {
            return AttachmentHandlerResult.Unchanged(message.Payload);
        }

        using var output = new MemoryStream();
        await dicom.SaveAsync(output);
        var rewritten = output.ToArray();

        return new AttachmentHandlerResult
        {
            RewrittenPayload = rewritten,
            Attachments = attachments,
            Extracted = true,
        };
    }

    private static byte[]? TryGetBytes(DicomDataset ds, DicomTag tag)
    {
        if (!ds.TryGetValues<byte>(tag, out var bytes) || bytes is null || bytes.Length == 0)
        {
            // Try string representations as fallback.
            if (ds.TryGetSingleValue<string>(tag, out var s) && !string.IsNullOrEmpty(s))
            {
                return Encoding.UTF8.GetBytes(s);
            }
            return null;
        }
        return bytes;
    }

    private static (IReadOnlyList<DicomTag> Tags, string MimeType) ParseProperties(AttachmentHandlerContext ctx)
    {
        var mime = string.IsNullOrWhiteSpace(ctx.ChannelMimeType) ? "application/dicom" : ctx.ChannelMimeType;
        var tags = new List<DicomTag>();

        if (!string.IsNullOrWhiteSpace(ctx.PropertiesJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(ctx.PropertiesJson);
                if (doc.RootElement.TryGetProperty("mimeType", out var m) && m.ValueKind == JsonValueKind.String)
                {
                    var s = m.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                        mime = s;
                }
                if (doc.RootElement.TryGetProperty("extractTags", out var arr) && arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in arr.EnumerateArray())
                    {
                        if (el.ValueKind != JsonValueKind.String)
                            continue;
                        var raw = el.GetString();
                        if (TryParseTag(raw, out var tag))
                            tags.Add(tag);
                    }
                }
            }
            catch (JsonException)
            {
                // fall through to default
            }
        }

        if (tags.Count == 0)
            tags.Add(_defaultPixelData);
        return (tags, mime);
    }

    private static bool TryParseTag(string? raw, out DicomTag tag)
    {
        tag = null!;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        var parts = raw.Replace("(", "").Replace(")", "").Split(',');
        if (parts.Length != 2)
            return false;
        if (!ushort.TryParse(parts[0].Trim(), NumberStyles.HexNumber, null, out var group))
            return false;
        if (!ushort.TryParse(parts[1].Trim(), NumberStyles.HexNumber, null, out var element))
            return false;
        tag = new DicomTag(group, element);
        return true;
    }
}
