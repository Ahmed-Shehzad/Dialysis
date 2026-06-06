namespace Dialysis.EHR.Billing.ChargeEdits;

/// <summary>
/// Deterministic, config-driven charge-review edits run before a charge reaches a claim — frequency
/// limits and required-diagnosis coverage. In-context (no external coding-edit DB), mirroring the
/// clinical safety checker.
/// </summary>
public interface IChargeEditChecker
{
    /// <summary>
    /// Evaluates a charge against the configured edits. <paramref name="payerCode"/> (the claim's payer,
    /// or null at capture time) drives the Medicare ABN escalation.
    /// </summary>
    Task<ChargeAdvisoryResult> CheckChargeAsync(
        Guid patientId,
        string cptCode,
        IReadOnlyList<string> diagnosisPointerIcd10Codes,
        string? payerCode,
        CancellationToken cancellationToken = default);
}
