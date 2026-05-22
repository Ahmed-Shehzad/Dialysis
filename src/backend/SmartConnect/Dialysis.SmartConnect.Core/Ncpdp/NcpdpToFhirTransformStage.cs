using System.Collections.Frozen;
using System.Text;
using Dialysis.SmartConnect.DataTypes.Ncpdp;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.SmartConnect.Ncpdp;

/// <summary>
/// Slice K2: parses the inbound payload as an NCPDP Telecom message, dispatches to the
/// <see cref="INcpdpToFhirMapper"/> matching the transaction code, and emits the
/// resulting FHIR R4 resource as JSON. When no mapper is registered for the transaction
/// (or the payload isn't Telecom) the payload passes through unchanged so a downstream
/// route can decide how to handle the unexpected shape.
/// </summary>
public sealed class NcpdpToFhirTransformStage : ITransformStage
{
    public const string KindValue = "ncpdp-to-fhir";

    private static readonly FhirJsonSerializer _serializer = new();

    private readonly FrozenDictionary<string, INcpdpToFhirMapper> _byTransactionCode;

    public NcpdpToFhirTransformStage(IEnumerable<INcpdpToFhirMapper> mappers)
    {
        ArgumentNullException.ThrowIfNull(mappers);
        _byTransactionCode = mappers.ToFrozenDictionary(
            m => m.TransactionCode,
            StringComparer.OrdinalIgnoreCase);
    }

    public string Kind => KindValue;

    public Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken)
        => Task.FromResult(Transform(message));

    // The transform itself is purely CPU-bound (NCPDP parse + FHIR serialize on in-memory data).
    // Splitting the work out of the *Async-named contract method lets us call Firely's
    // synchronous SerializeToString here without VSTHRD103 — its *Async sibling is [Obsolete]
    // (CodeQL cs/call-to-obsolete-method), so the analyzer would otherwise force us to suppress.
    private IntegrationMessage Transform(IntegrationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var text = Encoding.UTF8.GetString(message.Payload.Span);
        var parsed = NcpdpTelecomMessage.TryParse(text);
        if (parsed is null || string.IsNullOrWhiteSpace(parsed.TransactionCode))
        {
            return message;
        }

        if (!_byTransactionCode.TryGetValue(parsed.TransactionCode, out var mapper))
        {
            return message;
        }

        var resource = mapper.Map(parsed);
        if (resource is null)
        {
            return message;
        }

        var json = _serializer.SerializeToString(resource);
        return message.CloneWithPayload(Encoding.UTF8.GetBytes(json), PayloadFormat.Utf8Text);
    }
}
