using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.Module.Contracts.Billing;
using Dialysis.PDMS.Contracts.Integration;
using Microsoft.Extensions.Logging;

namespace Dialysis.EHR.Billing.Consumers;

/// <summary>
/// Listens for <see cref="DialysisSessionChargeReadyIntegrationEvent"/> from PDMS and
/// captures the matching <see cref="Charge"/> aggregate. Idempotent on
/// <c>(SessionId, CptCode)</c> via <see cref="IChargeIdempotencyStore"/>: re-delivery of
/// the same event finds the existing row and exits without creating a duplicate.
///
/// The CPT-coded amount comes from <see cref="ICptFeeSchedule"/> — production
/// deployments wire in their negotiated payer-specific fee schedule; the default
/// implementation reads from configuration so deployments without a real schedule still
/// get a deterministic billed amount.
///
/// Diagnosis pointers default to the dialysis ICD-10 codes the rest of EHR.Billing
/// expects (N18.6 — ESRD requiring chronic dialysis). The encounter id reuses the PDMS
/// session id one-to-one: PDMS sessions and EHR encounters are the same clinical event
/// from two different vantage points.
/// </summary>
public sealed class DialysisSessionChargeReadyConsumer : IConsumer<DialysisSessionChargeReadyIntegrationEvent>
{
    private readonly IChargeRepository _charges;
    private readonly IChargeIdempotencyStore _idempotency;
    private readonly ITransponderBus _bus;
    private readonly TimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DialysisSessionChargeReadyConsumer> _logger;
    /// <summary>
    /// Listens for <see cref="DialysisSessionChargeReadyIntegrationEvent"/> from PDMS and
    /// captures the matching <see cref="Charge"/> aggregate. Idempotent on
    /// <c>(SessionId, CptCode)</c> via <see cref="IChargeIdempotencyStore"/>: re-delivery of
    /// the same event finds the existing row and exits without creating a duplicate.
    ///
    /// The amount is priced from the shared <see cref="DialysisTariff"/> (setup + per-minute +
    /// per-litre UF) — the same calculation PDMS streams as the live chairside estimate, so the
    /// invoice total matches what the clinician watched accrue. The priced lines are then handed
    /// to HIE Documents via <see cref="DialysisInvoiceReadyIntegrationEvent"/>, which renders the
    /// AcroForm invoice PDF.
    ///
    /// Diagnosis pointers default to the dialysis ICD-10 codes the rest of EHR.Billing
    /// expects (N18.6 — ESRD requiring chronic dialysis). The encounter id reuses the PDMS
    /// session id one-to-one: PDMS sessions and EHR encounters are the same clinical event
    /// from two different vantage points.
    /// </summary>
    public DialysisSessionChargeReadyConsumer(IChargeRepository charges,
        IChargeIdempotencyStore idempotency,
        ITransponderBus bus,
        TimeProvider clock,
        IUnitOfWork unitOfWork,
        ILogger<DialysisSessionChargeReadyConsumer> logger)
    {
        _charges = charges;
        _idempotency = idempotency;
        _bus = bus;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    public async Task HandleAsync(ConsumeContext<DialysisSessionChargeReadyIntegrationEvent> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;
        var existing = await _idempotency.FindChargeIdAsync(message.SessionId, message.CptCode, ct)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            _logger.LogDebug(
                "Charge for session {SessionId} CPT {CptCode} already exists ({ChargeId}); skipping.",
                message.SessionId, message.CptCode, existing);
            return;
        }

        // Price the session into itemised lines using the shared tariff — the same calculation
        // PDMS uses for the live chairside estimate, so the invoice total matches what the
        // clinician watched accrue during treatment.
        var breakdown = DialysisTariff.Compute(
            message.Modality, message.DurationMinutes, message.AchievedUfVolumeLiters);
        var billed = new Money(breakdown.Total, breakdown.CurrencyCode);
        var diagnosisPointers = DialysisDiagnosisDefaults.PointersFor();

        var chargeId = Guid.CreateVersion7();
        var charge = Charge.Capture(
            id: chargeId,
            patientId: message.PatientId,
            encounterId: message.SessionId,
            cptCode: message.CptCode,
            diagnosisPointerIcd10Codes: diagnosisPointers,
            billedAmount: billed);
        _charges.Add(charge);
        await _idempotency.RegisterAsync(message.SessionId, message.CptCode, chargeId, ct).ConfigureAwait(false);

        // Hand the priced lines to HIE Documents, which renders the AcroForm invoice PDF.
        var issuedAt = _clock.GetUtcNow().UtcDateTime;
        var invoiceNumber =
            $"INV-{message.CompletedAtUtc:yyyyMMdd}-{message.SessionId.ToString("N")[..6].ToUpperInvariant()}";
        await _bus.PublishAsync(
            new DialysisInvoiceReadyIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                OccurredOn: issuedAt,
                SchemaVersion: 1,
                ChargeId: chargeId,
                PatientId: message.PatientId,
                SessionId: message.SessionId,
                InvoiceNumber: invoiceNumber,
                IssueDateUtc: issuedAt,
                Modality: message.Modality,
                CptCode: message.CptCode,
                DurationMinutes: message.DurationMinutes,
                Total: breakdown.Total,
                CurrencyCode: breakdown.CurrencyCode,
                Lines: breakdown.Lines
                    .Select(l => new InvoiceLineDto(l.Label, l.Quantity, l.Unit, l.UnitPrice, l.Amount))
                    .ToList()),
            ct).ConfigureAwait(false);

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Captured Charge {ChargeId} ({Amount} {Currency}) and queued invoice {InvoiceNumber} for session {SessionId}.",
            chargeId, billed.Amount, billed.CurrencyCode, invoiceNumber, message.SessionId);
    }
}

/// <summary>
/// Resolves the billed amount for a CPT code. Production deployments back this with their
/// negotiated payer-specific fee schedules; smaller installs use a static configuration
/// table.
/// </summary>
public interface ICptFeeSchedule
{
    Task<Money> LookupAsync(string cptCode, CancellationToken cancellationToken);
}

/// <summary>
/// Diagnosis-pointer defaults per modality. PDMS publishes only the modality; EHR.Billing
/// supplies the ICD-10 anchor codes that pair with the CPT lines on the EDI 837 claim.
/// </summary>
internal static class DialysisDiagnosisDefaults
{
    // Every modality today (HD, PD, and their spelling variants) anchors to the same ESRD
    // diagnosis — ICD-10 N18.6, "end-stage renal disease requiring chronic dialysis".
    // Reintroduce a modality switch here when a modality needs a different pointer set.
    public static IReadOnlyList<string> PointersFor() => ["N18.6"];
}
