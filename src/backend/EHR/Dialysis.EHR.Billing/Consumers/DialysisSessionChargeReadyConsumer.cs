using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;
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
    private readonly ICptFeeSchedule _feeSchedule;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DialysisSessionChargeReadyConsumer> _logger;
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
    public DialysisSessionChargeReadyConsumer(IChargeRepository charges,
        IChargeIdempotencyStore idempotency,
        ICptFeeSchedule feeSchedule,
        IUnitOfWork unitOfWork,
        ILogger<DialysisSessionChargeReadyConsumer> logger)
    {
        _charges = charges;
        _idempotency = idempotency;
        _feeSchedule = feeSchedule;
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

        var billed = await _feeSchedule.LookupAsync(message.CptCode, ct).ConfigureAwait(false);
        var diagnosisPointers = DialysisDiagnosisDefaults.PointersFor(message.Modality);

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
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Captured Charge {ChargeId} for session {SessionId} CPT {CptCode} amount {Amount} {Currency}.",
            chargeId, message.SessionId, message.CptCode, billed.Amount, billed.CurrencyCode);
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
    public static IReadOnlyList<string> PointersFor(string modality) => modality switch
    {
        // ESRD requiring chronic dialysis — the universal anchor diagnosis for HD claims.
        var m when m.Equals("HD", StringComparison.OrdinalIgnoreCase) => ["N18.6"],
        var m when m.Contains("haemo", StringComparison.OrdinalIgnoreCase) => ["N18.6"],
        var m when m.Contains("hemo", StringComparison.OrdinalIgnoreCase) => ["N18.6"],
        // Peritoneal dialysis encounter; same anchor diagnosis applies.
        var m when m.Equals("PD", StringComparison.OrdinalIgnoreCase) => ["N18.6"],
        var m when m.Contains("peritoneal", StringComparison.OrdinalIgnoreCase) => ["N18.6"],
        _ => ["N18.6"],
    };
}
