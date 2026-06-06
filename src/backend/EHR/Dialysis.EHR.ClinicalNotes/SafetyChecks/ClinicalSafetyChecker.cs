using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Ports;
using Microsoft.Extensions.Options;

namespace Dialysis.EHR.ClinicalNotes.SafetyChecks;

/// <summary>
/// Deterministic point-of-care safety checker. Reads the patient's own chart (allergies, medication
/// statements, prescriptions, recent lab orders) and raises advisories for medication↔allergy
/// conflicts, duplicate active medications, and duplicate recent lab orders.
///
/// <para>The concept match mirrors the cross-source reconciliation logic the HIE insights builder uses,
/// but is re-implemented here over the EHR's own string/<see cref="Coding"/> shapes — the module
/// boundary bars referencing the HIE assembly, and the logic is small and stable.</para>
/// </summary>
public sealed class ClinicalSafetyChecker : IClinicalSafetyChecker
{
    private readonly IAllergyRepository _allergies;
    private readonly IMedicationStatementRepository _medications;
    private readonly IPrescriptionRepository _prescriptions;
    private readonly ILabOrderRepository _labOrders;
    private readonly TimeProvider _timeProvider;
    private readonly ClinicalSafetyOptions _options;

    public ClinicalSafetyChecker(
        IAllergyRepository allergies,
        IMedicationStatementRepository medications,
        IPrescriptionRepository prescriptions,
        ILabOrderRepository labOrders,
        TimeProvider timeProvider,
        IOptions<ClinicalSafetyOptions> options)
    {
        _allergies = allergies;
        _medications = medications;
        _prescriptions = prescriptions;
        _labOrders = labOrders;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public async Task<SafetyAdvisoryResult> CheckPrescriptionAsync(
        Guid patientId,
        string medicationRxnormCode,
        string medicationDisplay,
        CancellationToken cancellationToken = default)
    {
        var ordered = (Code: medicationRxnormCode?.Trim() ?? string.Empty, Display: medicationDisplay?.Trim() ?? string.Empty);
        var advisories = new List<SafetyAdvisory>();

        var allergies = await _allergies.ListByPatientAsync(patientId, cancellationToken).ConfigureAwait(false);
        foreach (var allergy in allergies)
        {
            // Refuted / entered-in-error allergies are not active contraindications.
            if (allergy.VerificationStatus is AllergyVerificationStatus.Refuted or AllergyVerificationStatus.EnteredInError)
                continue;
            if (!ConceptsMatch(ordered.Code, ordered.Display, allergy.Allergen.Code, allergy.Allergen.Display))
                continue;
            advisories.Add(new SafetyAdvisory(
                AdvisoryCategory.MedicationAllergyConflict,
                _options.MedicationAllergyConflictBlocks ? AdvisorySeverity.Blocking : AdvisorySeverity.Warning,
                allergy.Allergen.Code,
                allergy.Allergen.Display ?? allergy.Allergen.Code,
                ordered.Display.Length > 0 ? ordered.Display : ordered.Code,
                allergy.Id,
                "Allergy"));
        }

        var activeMeds = await _medications.ListByPatientAsync(patientId, activeOnly: true, cancellationToken).ConfigureAwait(false);
        foreach (var med in activeMeds)
        {
            if (!ConceptsMatch(ordered.Code, ordered.Display, med.Medication.Code, med.Medication.Display))
                continue;
            advisories.Add(new SafetyAdvisory(
                AdvisoryCategory.DuplicateActiveMedication,
                AdvisorySeverity.Warning,
                med.Medication.Code,
                med.Medication.Display ?? med.Medication.Code,
                ordered.Display.Length > 0 ? ordered.Display : ordered.Code,
                med.Id,
                "MedicationStatement"));
        }

        var activePrescriptions = await _prescriptions.ListActiveByPatientAsync(patientId, cancellationToken).ConfigureAwait(false);
        foreach (var rx in activePrescriptions)
        {
            if (!ConceptsMatch(ordered.Code, ordered.Display, rx.MedicationRxnormCode, rx.MedicationDisplay))
                continue;
            advisories.Add(new SafetyAdvisory(
                AdvisoryCategory.DuplicateActiveMedication,
                AdvisorySeverity.Warning,
                rx.MedicationRxnormCode,
                rx.MedicationDisplay,
                ordered.Display.Length > 0 ? ordered.Display : ordered.Code,
                rx.Id,
                "Prescription"));
        }

        DetectInteractions(ordered, activeMeds, activePrescriptions, advisories);

        return new SafetyAdvisoryResult(advisories);
    }

    /// <summary>
    /// Raises a <see cref="AdvisoryCategory.DrugInteraction"/> advisory for each current medication that
    /// interacts with the ordered drug per a configured <see cref="DrugInteractionRule"/>. No-op when no
    /// rules are configured.
    /// </summary>
    private void DetectInteractions(
        (string Code, string Display) ordered,
        IReadOnlyList<MedicationStatement> activeMeds,
        IReadOnlyList<Prescription> activePrescriptions,
        List<SafetyAdvisory> advisories)
    {
        if (_options.DrugInteractions.Count == 0)
            return;

        var current = new List<(string Code, string? Display, Guid Id, string Kind)>();
        current.AddRange(activeMeds.Select(m => (m.Medication.Code, m.Medication.Display, m.Id, "MedicationStatement")));
        current.AddRange(activePrescriptions.Select(rx => (rx.MedicationRxnormCode, (string?)rx.MedicationDisplay, rx.Id, "Prescription")));

        var seen = new HashSet<Guid>();
        foreach (var rule in _options.DrugInteractions)
        {
            var orderedFirst = ConceptsMatch(ordered.Code, ordered.Display, rule.FirstCode, rule.FirstDisplay);
            var orderedSecond = ConceptsMatch(ordered.Code, ordered.Display, rule.SecondCode, rule.SecondDisplay);
            if (!orderedFirst && !orderedSecond)
                continue;

            foreach (var cur in current)
            {
                var hit = (orderedFirst && ConceptsMatch(cur.Code, cur.Display, rule.SecondCode, rule.SecondDisplay))
                    || (orderedSecond && ConceptsMatch(cur.Code, cur.Display, rule.FirstCode, rule.FirstDisplay));
                if (!hit || !seen.Add(cur.Id))
                    continue;
                advisories.Add(new SafetyAdvisory(
                    AdvisoryCategory.DrugInteraction,
                    rule.Blocking ? AdvisorySeverity.Blocking : AdvisorySeverity.Warning,
                    cur.Code,
                    cur.Display ?? cur.Code,
                    ordered.Display.Length > 0 ? ordered.Display : ordered.Code,
                    cur.Id,
                    cur.Kind,
                    rule.Description));
            }
        }
    }

    public async Task<SafetyAdvisoryResult> CheckLabOrderAsync(
        Guid patientId,
        IReadOnlyList<string> loincPanelCodes,
        CancellationToken cancellationToken = default)
    {
        var ordered = (loincPanelCodes ?? [])
            .Select(c => c?.Trim() ?? string.Empty)
            .Where(c => c.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (ordered.Count == 0)
            return SafetyAdvisoryResult.None;

        var sinceUtc = _timeProvider.GetUtcNow().UtcDateTime.AddHours(-Math.Abs(_options.DuplicateLabWindowHours));
        var recent = await _labOrders.ListRecentByPatientAsync(patientId, sinceUtc, cancellationToken).ConfigureAwait(false);

        var advisories = new List<SafetyAdvisory>();
        foreach (var order in recent)
        {
            var overlap = order.LoincPanelCodes.FirstOrDefault(c => ordered.Contains(c));
            if (overlap is null)
                continue;
            advisories.Add(new SafetyAdvisory(
                AdvisoryCategory.DuplicateLabOrder,
                AdvisorySeverity.Warning,
                overlap,
                overlap,
                overlap,
                order.Id,
                "LabOrder"));
        }

        return new SafetyAdvisoryResult(advisories);
    }

    /// <summary>
    /// Deterministic concept match: a shared non-empty code (case-insensitive), or one display
    /// containing the other (e.g. allergy "Penicillin" vs ordered "Penicillin V"). No external
    /// drug-knowledge base.
    /// </summary>
    private static bool ConceptsMatch(string aCode, string? aDisplay, string bCode, string? bDisplay)
    {
        if (!string.IsNullOrWhiteSpace(aCode) && !string.IsNullOrWhiteSpace(bCode) &&
            string.Equals(aCode, bCode, StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.IsNullOrWhiteSpace(aDisplay) || string.IsNullOrWhiteSpace(bDisplay))
            return false;
        return aDisplay.Contains(bDisplay, StringComparison.OrdinalIgnoreCase)
            || bDisplay.Contains(aDisplay, StringComparison.OrdinalIgnoreCase);
    }
}
