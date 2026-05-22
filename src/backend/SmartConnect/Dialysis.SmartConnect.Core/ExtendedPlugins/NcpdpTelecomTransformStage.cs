using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.DataTypes.Ncpdp;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>
/// Slice K of the SmartConnect ↔ Mirth alignment plan: parses NCPDP Telecom Standard (5.1
/// / D.0 / D.1) payloads into a JSON-addressable form so downstream JSONPath / mapper /
/// JavaScript transforms can route pharmacy claims, eligibility verifications, and
/// reversals by transaction code / NDC / patient without writing custom byte-level
/// parsers. Mirth UG lists NCPDP as a first-class data type; SmartConnect previously
/// required JS regex for these messages.
/// </summary>
/// <remarks>
/// Output shape:
/// <code>
/// {
///   "transactionCode": "B1",
///   "versionRelease": "D0",
///   "segments": [
///     { "index": 0, "segmentId": null, "fields": { "A1": "...", "A2": "D0", "A3": "B1", ... } },
///     { "index": 1, "segmentId": "AM01", "fields": { "AM": "01", "C4": "...", ... } },
///     ...
///   ]
/// }
/// </code>
/// Non-Telecom payloads (no segment separator characters in the body) pass through
/// untouched — the downstream route can decide how to handle the unexpected format.
/// </remarks>
public sealed class NcpdpTelecomTransformStage : ITransformStage
{
    public const string KindValue = "ncpdp-telecom";

    public string Kind => KindValue;

    public Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var payloadText = Encoding.UTF8.GetString(message.Payload.Span);
        var parsed = NcpdpTelecomMessage.TryParse(payloadText);
        if (parsed is null)
            return Task.FromResult(message);

        var json = ProjectMessage(parsed);
        return Task.FromResult(message.CloneWithPayload(Encoding.UTF8.GetBytes(json), PayloadFormat.Utf8Text));
    }

    private static string ProjectMessage(NcpdpTelecomMessage parsed)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("transactionCode", parsed.TransactionCode);
            writer.WriteString("versionRelease", parsed.VersionRelease);

            writer.WritePropertyName("segments");
            writer.WriteStartArray();
            foreach (var segment in parsed.Segments)
            {
                writer.WriteStartObject();
                writer.WriteNumber("index", segment.Index);
                writer.WriteString("segmentId", segment.SegmentId);

                writer.WritePropertyName("fields");
                writer.WriteStartObject();
                foreach (var (key, value) in segment.Fields)
                {
                    writer.WriteString(key, value);
                }
                writer.WriteEndObject();

                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
