using Dialysis.EHR.ClinicalNotes.SafetyChecks;

namespace Dialysis.EHR.ClinicalNotes.Ports;

/// <summary>
/// Point-of-care safety checks run against the patient's own chart at order entry. Deterministic and
/// in-context (no external drug-knowledge base, no cross-module dependency). See
/// <see cref="SafetyAdvisory"/> for the signal shape.
/// </summary>
public interface IClinicalSafetyChecker
{
    /// <summary>
    /// Checks a prospective prescription for a medication↔allergy conflict and duplicate active
    /// medications.
    /// </summary>
    Task<SafetyAdvisoryResult> CheckPrescriptionAsync(
        Guid patientId,
        string medicationRxnormCode,
        string medicationDisplay,
        CancellationToken cancellationToken = default);

    /// <summary>Checks a prospective lab order for a duplicate of a recently-ordered panel.</summary>
    Task<SafetyAdvisoryResult> CheckLabOrderAsync(
        Guid patientId,
        IReadOnlyList<string> loincPanelCodes,
        CancellationToken cancellationToken = default);
}
