using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Contracts.Messaging;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.HIS.Contracts.IntegrationEvents.Billing;
using Microsoft.Extensions.Logging;

namespace Dialysis.EHR.Billing.Consumers;

/// <summary>
/// Listens for <see cref="BillingExportJobQueuedIntegrationEvent"/> from HIS — the facility-operations
/// trigger for a payer-billing window — and runs the EDI 837 export on the EHR side (EHR.Billing is the
/// authoritative claims pipeline; HIS only owns the queue).
///
/// Export = gather the payer's ready-to-ship claims (status <see cref="ClaimStatus.Assembled"/>), stamp
/// each with an EDI control number and transition it to <see cref="ClaimStatus.Submitted"/> (which raises
/// <see cref="ClaimSubmittedIntegrationEvent"/> per claim, drained to the outbox on save), then report the
/// batch outcome back to HIS via <see cref="BillingExportJobCompletedIntegrationEvent"/> (or
/// <see cref="BillingExportJobFailedIntegrationEvent"/> on error) so the HIS export job leaves <c>Queued</c>.
/// A zero-claim window is a valid, successful no-op batch.
///
/// Two deliberate simplifications: (1) claims are matched by payer code only — an <c>Assembled</c> claim
/// carries no submission date, so the HIS-supplied period is recorded on the job but not used to slice the
/// not-yet-shipped backlog; (2) charge-edit advisories are evaluated at assembly/<c>SubmitClaim</c> time, not
/// re-run here — the export is the transmission step for claims a biller already assembled.
/// </summary>
public sealed class BillingExportJobQueuedConsumer : IConsumer<BillingExportJobQueuedIntegrationEvent>
{
    private const int BatchScanLimit = 500;

    private readonly IClaimRepository _claims;
    private readonly ITransponderOutbox _outbox;
    private readonly ITransponderBus _bus;
    private readonly TimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BillingExportJobQueuedConsumer> _logger;

    /// <summary>
    /// Listens for <see cref="BillingExportJobQueuedIntegrationEvent"/> from HIS, submits the payer's
    /// assembled claims as the EDI 837 batch, and reports the outcome back to HIS.
    /// </summary>
    public BillingExportJobQueuedConsumer(IClaimRepository claims,
        ITransponderOutbox outbox,
        ITransponderBus bus,
        TimeProvider clock,
        IUnitOfWork unitOfWork,
        ILogger<BillingExportJobQueuedConsumer> logger)
    {
        _claims = claims;
        _outbox = outbox;
        _bus = bus;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(ConsumeContext<BillingExportJobQueuedIntegrationEvent> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;
        var nowUtc = _clock.GetUtcNow().UtcDateTime;
        var payerCode = message.PayerCode.Trim().ToUpperInvariant();

        try
        {
            // Gather the payer's ready-to-ship claims into the export batch (tracked, so Submit persists).
            var ready = await _claims.ListAsync(ClaimStatus.Assembled, BatchScanLimit, ct).ConfigureAwait(false);
            var batch = ready
                .Where(c => string.Equals(c.PayerCode, payerCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Transmit: stamp each claim with an EDI 837 control number and move it to Submitted.
            // Each Submit raises ClaimSubmittedIntegrationEvent, drained to the outbox on SaveChanges.
            foreach (var claim in batch)
            {
                claim.Submit(GenerateControlNumber(nowUtc), nowUtc);
            }

            var currency = batch.Count > 0 ? batch[0].BilledTotal.CurrencyCode : "USD";
            var billedTotal = batch
                .Where(c => string.Equals(c.BilledTotal.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase))
                .Sum(c => c.BilledTotal.Amount);

            // The batch outcome rides the transactional outbox: the claim submissions and the
            // completed signal commit together, so HIS can never see submitted claims while its
            // export job is stranded in Queued (or the reverse).
            await _outbox.EnqueueAsync(
                HisTransponderOutboxEnvelope.From(new BillingExportJobCompletedIntegrationEvent(
                    EventId: Guid.CreateVersion7(),
                    OccurredOn: nowUtc,
                    SchemaVersion: 1,
                    JobId: message.JobId,
                    PayerCode: payerCode,
                    ClaimCount: batch.Count,
                    BilledTotal: billedTotal,
                    CurrencyCode: currency,
                    CompletedAtUtc: nowUtc)),
                ct).ConfigureAwait(false);

            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Submitted billing export batch for job {JobId} payer {PayerCode}: {ClaimCount} claim(s), {BilledTotal} {Currency}.",
                message.JobId, payerCode, batch.Count, billedTotal, currency);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Failed to run billing export batch for job {JobId} payer {PayerCode}.",
                message.JobId, payerCode);

            // Deliberately published directly: the unit of work just failed/rolled back, so an
            // outbox row would roll back with it — the failure signal must escape the dead
            // transaction for HIS to move the job out of Queued.
            await _bus.PublishAsync(
                new BillingExportJobFailedIntegrationEvent(
                    EventId: Guid.CreateVersion7(),
                    OccurredOn: nowUtc,
                    SchemaVersion: 1,
                    JobId: message.JobId,
                    PayerCode: payerCode,
                    Reason: ex.Message,
                    FailedAtUtc: nowUtc),
                ct).ConfigureAwait(false);
        }
    }

    // EDI 837 ISA/GS control number — same timestamp + short-GUID shape SubmitClaimCommandHandler uses.
    private static string GenerateControlNumber(DateTime utcNow) =>
        $"{utcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
