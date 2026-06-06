using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;
using Dialysis.EHR.Contracts.Integration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.EHR.Billing.Consumers;

/// <summary>
/// Auto-captures professional charges when a clinical encounter closes: one <see cref="Charge"/> per
/// procedure CPT on the encounter, priced via <see cref="ICptFeeSchedule"/>, with the encounter's
/// diagnoses as the pointers. Opt-in via <see cref="EncounterChargeAutomationOptions"/> (default off).
///
/// <para>Idempotent on <c>(EncounterId, CptCode)</c> via <see cref="IChargeIdempotencyStore"/> — the
/// same store the PDMS session consumer uses (the "session" slot carries the encounter id). Re-delivery
/// finds the existing row and skips. A CPT with no fee-schedule row is logged and skipped (it surfaces
/// as a lost-charge worklist item rather than poisoning the whole encounter).</para>
/// </summary>
public sealed class EncounterClosedChargeConsumer : IConsumer<EncounterClosedIntegrationEvent>
{
    private readonly IChargeRepository _charges;
    private readonly IChargeIdempotencyStore _idempotency;
    private readonly ICptFeeSchedule _fees;
    private readonly IUnitOfWork _unitOfWork;
    private readonly EncounterChargeAutomationOptions _options;
    private readonly ILogger<EncounterClosedChargeConsumer> _logger;

    public EncounterClosedChargeConsumer(IChargeRepository charges,
        IChargeIdempotencyStore idempotency,
        ICptFeeSchedule fees,
        IUnitOfWork unitOfWork,
        IOptions<EncounterChargeAutomationOptions> options,
        ILogger<EncounterClosedChargeConsumer> logger)
    {
        _charges = charges;
        _idempotency = idempotency;
        _fees = fees;
        _unitOfWork = unitOfWork;
        _options = options.Value;
        _logger = logger;
    }

    public async Task HandleAsync(ConsumeContext<EncounterClosedIntegrationEvent> context)
    {
        if (!_options.Enabled)
            return;

        var message = context.Message;
        var ct = context.CancellationToken;

        var diagnosisPointers = message.DiagnosisIcd10Codes
            .Select(c => c?.Trim() ?? string.Empty)
            .Where(c => c.Length > 0)
            .ToList();
        if (diagnosisPointers.Count == 0)
        {
            // Encounter.Close() enforces >= 1 diagnosis, so this is defensive; a charge can't be
            // captured without a pointer.
            _logger.LogWarning(
                "Encounter {EncounterId} closed with no diagnosis codes; skipping auto charge capture.",
                message.EncounterId);
            return;
        }

        var captured = 0;
        foreach (var cpt in message.ProcedureCptCodes
            .Select(c => c?.Trim() ?? string.Empty)
            .Where(c => c.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var existing = await _idempotency.FindChargeIdAsync(message.EncounterId, cpt, ct).ConfigureAwait(false);
            if (existing is not null)
                continue;

            Money billed;
            try
            {
                billed = await _fees.LookupAsync(cpt, ct).ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
                // No fee-schedule row for this CPT — don't poison the message; the encounter shows up
                // on the lost-charge worklist until a fee is configured and the event is replayed.
                _logger.LogWarning(
                    "No fee-schedule entry for CPT {CptCode} on encounter {EncounterId}; skipping that line.",
                    cpt, message.EncounterId);
                continue;
            }

            var chargeId = Guid.CreateVersion7();
            var charge = Charge.Capture(
                id: chargeId,
                patientId: message.PatientId,
                encounterId: message.EncounterId,
                cptCode: cpt,
                diagnosisPointerIcd10Codes: diagnosisPointers,
                billedAmount: billed);
            _charges.Add(charge);
            await _idempotency.RegisterAsync(message.EncounterId, cpt, chargeId, ct).ConfigureAwait(false);
            captured++;
        }

        if (captured > 0)
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Auto-captured {Count} charge(s) for closed encounter {EncounterId}.",
            captured, message.EncounterId);
    }
}
