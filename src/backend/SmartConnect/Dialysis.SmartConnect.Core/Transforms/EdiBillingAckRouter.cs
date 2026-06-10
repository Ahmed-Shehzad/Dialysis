using System.Text;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;

namespace Dialysis.SmartConnect.Transforms;

/// <summary>
/// Inbound transform stage that classifies an ANSI ASC X12N acknowledgement payload as
/// either a 999 (functional ack) or a 277CA (claim ack) and republishes it as an
/// <see cref="EdiAcknowledgementReceivedIntegrationEvent"/> for EHR.Billing to consume.
///
/// Classification reads the ST01 transaction-set identifier in the second segment after
/// ISA/GS — 999 vs 277. We deliberately don't parse the body in SmartConnect; the parser
/// lives in EHR.Billing because the parsed shape feeds directly into the Claim aggregate's
/// state machine, and SmartConnect should stay free of EHR domain coupling.
///
/// The stage hands the payload through untouched (no mutation), so the downstream message
/// store still has the unaltered EDI for the operator audit trail.
/// </summary>
public sealed class EdiBillingAckRouter : ITransformStage
{
    private readonly ITransponderBus _bus;
    /// <summary>
    /// Inbound transform stage that classifies an ANSI ASC X12N acknowledgement payload as
    /// either a 999 (functional ack) or a 277CA (claim ack) and republishes it as an
    /// <see cref="EdiAcknowledgementReceivedIntegrationEvent"/> for EHR.Billing to consume.
    ///
    /// Classification reads the ST01 transaction-set identifier in the second segment after
    /// ISA/GS — 999 vs 277. We deliberately don't parse the body in SmartConnect; the parser
    /// lives in EHR.Billing because the parsed shape feeds directly into the Claim aggregate's
    /// state machine, and SmartConnect should stay free of EHR domain coupling.
    ///
    /// The stage hands the payload through untouched (no mutation), so the downstream message
    /// store still has the unaltered EDI for the operator audit trail.
    /// </summary>
    public EdiBillingAckRouter(ITransponderBus bus) => _bus = bus;
    public string Kind => "edi.billing.ack-router";

    public async Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var bytes = message.Payload;
        if (bytes.IsEmpty)
            return message;

        var ackKind = ClassifyAckKind(bytes.Span);
        if (ackKind is null)
            return message;

        var receivedAt = message.ReceivedAtUtc.UtcDateTime;
        var trace = message.Metadata.TryGetValue("source-trace", out var t) ? t : null;
        var integrationEvent = new EdiAcknowledgementReceivedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: receivedAt,
            SchemaVersion: 1,
            AckKind: ackKind.Value,
            PayloadBytes: bytes.ToArray(),
            ReceivedAtUtc: receivedAt,
            SourceTrace: trace);
        await _bus.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
        return message;
    }

    /// <summary>
    /// Reads the ST segment to recover the transaction-set identifier. X12 declares its
    /// delimiters in the ISA segment positions 3 and 105/106, so we honour those instead
    /// of assuming <c>*</c> / <c>~</c>.
    /// </summary>
    private static EdiAckKind? ClassifyAckKind(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 110)
            return null;
        var text = Encoding.ASCII.GetString(bytes);
        if (!text.StartsWith("ISA", StringComparison.Ordinal))
            return null;
        var elementSeparator = text[3];
        var segmentTerminator = text[105];

        // Walk segments until we find ST.
        var stIndex = text.IndexOf("ST" + elementSeparator, StringComparison.Ordinal);
        if (stIndex < 0)
            return null;
        var endOfSt = text.IndexOf(segmentTerminator, stIndex);
        if (endOfSt < 0)
            return null;
        var segment = text[stIndex..endOfSt];
        var parts = segment.Split(elementSeparator);
        if (parts.Length < 2)
            return null;
        return parts[1] switch
        {
            "999" => EdiAckKind.FunctionalAck999,
            "277" => EdiAckKind.ClaimAck277Ca,
            _ => null,
        };
    }
}
