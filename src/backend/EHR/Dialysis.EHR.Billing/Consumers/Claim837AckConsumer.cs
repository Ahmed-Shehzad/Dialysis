using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Edi837.Inbound;
using Dialysis.EHR.Billing.Ports;
using Dialysis.EHR.Contracts.Integration;
using Microsoft.Extensions.Logging;

namespace Dialysis.EHR.Billing.Consumers;

/// <summary>
/// Listens for inbound EDI 837 acknowledgement payloads. SmartConnect routes both 999
/// functional acks and 277CA claim acks here via a single
/// <see cref="Hl7FhirResourceReceivedIntegrationEvent"/>-style envelope that carries the
/// raw byte payload plus a hint about which ack kind it represents.
///
/// Routing decisions:
/// <list type="bullet">
///   <item>The 999 carries the original group + transaction control numbers — we look the
///         claim up by ExternalControlNumber (which we set to the GS control number on
///         submit) and record a <see cref="ClaimAckKind.FunctionalAck999"/> row.</item>
///   <item>The 277CA carries the original CLM01 — we look the claim up by its
///         <see cref="Claim.Id"/> (CLM01 = claim id in N format) and record a
///         <see cref="ClaimAckKind.ClaimAck277Ca"/> row per claim status block.</item>
/// </list>
///
/// Both ack kinds are idempotent — re-delivery is detected by inspecting the existing
/// claim's <see cref="Claim.Acknowledgements"/> history before appending.
/// </summary>
public sealed class Claim837AckConsumer(
    IClaimRepository claims,
    Edi999FunctionalAckParser ack999Parser,
    Edi277CaAckParser ack277Parser,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger<Claim837AckConsumer> logger)
    : IConsumer<EdiAcknowledgementReceivedIntegrationEvent>
{
    public async Task HandleAsync(ConsumeContext<EdiAcknowledgementReceivedIntegrationEvent> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;
        switch (message.AckKind)
        {
            case EdiAckKind.FunctionalAck999:
                await Handle999Async(message, ct).ConfigureAwait(false);
                break;
            case EdiAckKind.ClaimAck277Ca:
                await Handle277CaAsync(message, ct).ConfigureAwait(false);
                break;
            default:
                logger.LogWarning("Unknown ack kind {Kind}; ignoring.", message.AckKind);
                return;
        }
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task Handle999Async(EdiAcknowledgementReceivedIntegrationEvent message, CancellationToken ct)
    {
        var parsed = ack999Parser.Parse(message.PayloadBytes);
        var controlNumber = parsed.OriginalGroupControlNumber ?? parsed.OriginalTransactionControlNumber;
        if (string.IsNullOrWhiteSpace(controlNumber))
        {
            logger.LogWarning("999 ack {EventId} carries no control number; cannot correlate to a claim.", message.EventId);
            return;
        }
        var claim = await claims.FindByExternalControlNumberAsync(controlNumber, ct).ConfigureAwait(false);
        if (claim is null)
        {
            logger.LogWarning("999 ack references control number {Control} but no matching claim was found.", controlNumber);
            return;
        }
        var verdict = parsed.Verdict switch
        {
            Edi999Verdict.Accepted => ClaimAckVerdict.Accepted,
            Edi999Verdict.AcceptedWithErrors => ClaimAckVerdict.AcceptedWithWarnings,
            _ => ClaimAckVerdict.Rejected,
        };
        var receivedAt = clock.GetUtcNow().UtcDateTime;
        claim.RecordAcknowledgement(new ClaimAcknowledgement(
            id: Guid.CreateVersion7(),
            kind: ClaimAckKind.FunctionalAck999,
            verdict: verdict,
            payerClaimControlNumber: null,
            reasonCodes: parsed.Errors,
            receivedAtUtc: receivedAt));
    }

    private async Task Handle277CaAsync(EdiAcknowledgementReceivedIntegrationEvent message, CancellationToken ct)
    {
        var parsed = ack277Parser.Parse(message.PayloadBytes);
        var receivedAt = clock.GetUtcNow().UtcDateTime;
        foreach (var status in parsed.ClaimStatuses)
        {
            if (!Guid.TryParseExact(status.OriginalClaimControlNumber, "N", out var claimId))
            {
                logger.LogWarning("277CA references unparseable claim control number {Control}.", status.OriginalClaimControlNumber);
                continue;
            }
            var claim = await claims.GetAsync(claimId, ct).ConfigureAwait(false);
            if (claim is null)
            {
                logger.LogWarning("277CA references claim {ClaimId} but it does not exist.", claimId);
                continue;
            }
            var verdict = status.Verdict switch
            {
                Edi277Verdict.Accepted => ClaimAckVerdict.Accepted,
                Edi277Verdict.Pending => ClaimAckVerdict.AcceptedWithWarnings,
                _ => ClaimAckVerdict.Rejected,
            };
            claim.RecordAcknowledgement(new ClaimAcknowledgement(
                id: Guid.CreateVersion7(),
                kind: ClaimAckKind.ClaimAck277Ca,
                verdict: verdict,
                payerClaimControlNumber: status.PayerClaimControlNumber,
                reasonCodes: status.ReasonCodes,
                receivedAtUtc: receivedAt));
        }
    }
}
