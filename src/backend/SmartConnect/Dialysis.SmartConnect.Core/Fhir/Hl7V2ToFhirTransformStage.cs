using System.Text;
using Dialysis.SmartConnect.DataTypes;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.SmartConnect.Fhir;

/// <summary>
/// Source-transform stage that parses an HL7 v2.x payload, runs it through
/// <see cref="Hl7V2ToFhirPipeline"/> (12 registered mappers as of writing — ADT^A01/A04/A08/A40,
/// ORU^R01/R30/R40, ORM^O01, SIU^S12, MDM^T02, VXU^V04), and replaces the payload with a FHIR R4
/// <c>Bundle</c> (type = collection) containing every resource the matching mappers produced.
///
/// When the payload is not an HL7 v2 message (or no mapper matches its trigger), the message
/// passes through unchanged so a downstream route can handle the unexpected shape — mirrors the
/// fail-soft behaviour of <c>NcpdpToFhirTransformStage</c>.
/// </summary>
public sealed class Hl7V2ToFhirTransformStage(Hl7V2ToFhirPipeline pipeline) : ITransformStage
{
    public const string KindValue = "hl7-to-fhir-pipeline";

    private static readonly FhirJsonSerializer _serializer = new();

    public string Kind => KindValue;

    public Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken)
        => Task.FromResult(Transform(message));

    private IntegrationMessage Transform(IntegrationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var text = Encoding.UTF8.GetString(message.Payload.Span);
        if (string.IsNullOrEmpty(text) || !text.StartsWith("MSH", StringComparison.Ordinal))
        {
            return message;
        }

        Hl7V2Message parsed;
        try
        {
            parsed = Hl7V2Message.Parse(text);
        }
        catch (FormatException)
        {
            return message;
        }
        catch (ArgumentException)
        {
            return message;
        }

        var resources = pipeline.Transform(parsed);
        if (resources.Count == 0)
        {
            return message;
        }

        var bundle = new Bundle
        {
            Type = Bundle.BundleType.Collection,
            Timestamp = DateTimeOffset.UtcNow,
        };
        foreach (var resource in resources)
        {
            bundle.Entry.Add(new Bundle.EntryComponent { Resource = resource });
        }

        // Synchronous SerializeToString — async sibling is [Obsolete], same rationale as
        // NcpdpToFhirTransformStage.
        var json = _serializer.SerializeToString(bundle);
        return message.CloneWithPayload(Encoding.UTF8.GetBytes(json), PayloadFormat.Json);
    }
}
