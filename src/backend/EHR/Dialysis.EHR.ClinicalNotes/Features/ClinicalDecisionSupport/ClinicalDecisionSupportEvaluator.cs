using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.PatientChart.Ports;
using Microsoft.Extensions.Options;

namespace Dialysis.EHR.ClinicalNotes.Features.ClinicalDecisionSupport;

/// <summary>A point-of-care clinical recommendation fired by a <see cref="CdsRule"/>.</summary>
public sealed record CdsRecommendation(
    string RuleId,
    CdsSeverity Severity,
    string Title,
    string Detail,
    string? SuggestedActionKind,
    string? SuggestedActionCode);

/// <summary>
/// Evaluates configured condition-specific decision-support rules against the patient's chart (active
/// problems, medications, recent labs and vitals) and returns the recommendations that currently fire.
/// Deterministic and config-driven (empty → no recommendations); no external knowledge base.
/// </summary>
public interface IClinicalDecisionSupportEvaluator
{
    Task<IReadOnlyList<CdsRecommendation>> EvaluateAsync(Guid patientId, CancellationToken cancellationToken = default);
}

public sealed class ClinicalDecisionSupportEvaluator : IClinicalDecisionSupportEvaluator
{
    private readonly IProblemListRepository _problems;
    private readonly IMedicationStatementRepository _medications;
    private readonly IVitalSignRepository _vitals;
    private readonly ILabResultRepository _labResults;
    private readonly TimeProvider _timeProvider;
    private readonly CdsOptions _options;

    public ClinicalDecisionSupportEvaluator(
        IProblemListRepository problems,
        IMedicationStatementRepository medications,
        IVitalSignRepository vitals,
        ILabResultRepository labResults,
        TimeProvider timeProvider,
        IOptions<CdsOptions> options)
    {
        _problems = problems;
        _medications = medications;
        _vitals = vitals;
        _labResults = labResults;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<CdsRecommendation>> EvaluateAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        if (_options.Rules.Count == 0)
            return [];

        var problems = await _problems.ListByPatientAsync(patientId, false, cancellationToken).ConfigureAwait(false);
        var problemCodes = problems.Select(p => p.Condition.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var recommendations = new List<CdsRecommendation>();
        foreach (var rule in _options.Rules)
        {
            // A rule with no condition filter applies to everyone; otherwise the patient must carry one
            // of the listed problems for it to apply.
            if (rule.AppliesToAnyIcd10.Count > 0
                && !rule.AppliesToAnyIcd10.Exists(c => problemCodes.Contains(c.Trim())))
                continue;

            var fired = rule.TriggerKind switch
            {
                CdsTriggerKind.MissingLabWithinMonths => await MissingLabAsync(patientId, rule, cancellationToken).ConfigureAwait(false),
                CdsTriggerKind.AbnormalVitalThreshold => await AbnormalVitalAsync(patientId, rule, cancellationToken).ConfigureAwait(false),
                CdsTriggerKind.ConditionWithoutMedicationClass => await MissingMedicationAsync(patientId, rule, cancellationToken).ConfigureAwait(false),
                _ => false,
            };

            if (fired)
            {
                recommendations.Add(new CdsRecommendation(
                    rule.Id, rule.Severity, rule.Title, rule.Detail ?? string.Empty,
                    rule.SuggestedActionKind, rule.SuggestedActionCode));
            }
        }

        return recommendations;
    }

    private async Task<bool> MissingLabAsync(Guid patientId, CdsRule rule, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rule.ExpectedLoinc))
            return false;
        var months = Math.Max(1, rule.WithinMonths);
        var since = _timeProvider.GetUtcNow().UtcDateTime.AddMonths(-months);
        var labs = await _labResults.ListByPatientAsync(patientId, since, cancellationToken).ConfigureAwait(false);
        return !labs.Any(l => l.LoincCode.Equals(rule.ExpectedLoinc.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> AbnormalVitalAsync(Guid patientId, CdsRule rule, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rule.VitalLoinc))
            return false;
        var since = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-Math.Max(1, rule.VitalWithinDays));
        var readings = await _vitals.ListByPatientAsync(patientId, since, cancellationToken).ConfigureAwait(false);
        var latest = readings
            .Where(r => r.ObservationType.Code.Equals(rule.VitalLoinc.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.ObservedAtUtc)
            .FirstOrDefault();
        // No reading at all is not, by itself, an abnormal-vital alert (a missing-lab rule covers absence).
        return latest is not null && rule.Comparator.Matches(latest.Value, rule.ThresholdValue);
    }

    private async Task<bool> MissingMedicationAsync(Guid patientId, CdsRule rule, CancellationToken cancellationToken)
    {
        if (rule.MedicationCodePrefixAny.Count == 0)
            return false;
        var meds = await _medications.ListByPatientAsync(patientId, true, cancellationToken).ConfigureAwait(false);
        var prefixes = rule.MedicationCodePrefixAny.Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
        var hasClass = meds.Any(m => prefixes.Exists(p =>
            m.Medication.Code.StartsWith(p, StringComparison.OrdinalIgnoreCase)));
        return !hasClass;
    }
}
