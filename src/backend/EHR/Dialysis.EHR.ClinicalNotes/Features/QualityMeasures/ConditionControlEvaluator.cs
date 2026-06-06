using System.Globalization;
using Dialysis.EHR.ClinicalNotes.Features.ClinicalDecisionSupport;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.PatientChart.Ports;

namespace Dialysis.EHR.ClinicalNotes.Features.QualityMeasures;

/// <summary>The control outcome for one patient against one measure.</summary>
public enum PatientControlOutcome
{
    /// <summary>The patient is not in the measure's condition cohort.</summary>
    NotApplicable = 0,
    Controlled = 1,
    Uncontrolled = 2,

    /// <summary>The patient is in the cohort but has no qualifying reading in the window.</summary>
    NoData = 3,
}

/// <summary>One patient's control evaluation (outcome + the value that decided it, when any).</summary>
public sealed record PatientControlStatus(PatientControlOutcome Outcome, decimal? Value);

/// <summary>
/// Evaluates whether a single patient's condition is controlled for a given <see cref="ControlRule"/> —
/// the per-patient building block the population control roll-up runs across a cohort. Deterministic;
/// non-numeric readings are treated as no-data, never thrown.
/// </summary>
public interface IConditionControlEvaluator
{
    Task<PatientControlStatus> EvaluateAsync(Guid patientId, ControlRule rule, CancellationToken cancellationToken = default);
}

public sealed class ConditionControlEvaluator : IConditionControlEvaluator
{
    private readonly IProblemListRepository _problems;
    private readonly IVitalSignRepository _vitals;
    private readonly ILabResultRepository _labResults;
    private readonly TimeProvider _timeProvider;

    public ConditionControlEvaluator(
        IProblemListRepository problems,
        IVitalSignRepository vitals,
        ILabResultRepository labResults,
        TimeProvider timeProvider)
    {
        _problems = problems;
        _vitals = vitals;
        _labResults = labResults;
        _timeProvider = timeProvider;
    }

    public async Task<PatientControlStatus> EvaluateAsync(Guid patientId, ControlRule rule, CancellationToken cancellationToken = default)
    {
        if (rule.AppliesToAnyIcd10.Count > 0)
        {
            var problems = await _problems.ListByPatientAsync(patientId, false, cancellationToken).ConfigureAwait(false);
            var codes = problems.Select(p => p.Condition.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!rule.AppliesToAnyIcd10.Any(c => codes.Contains(c.Trim())))
                return new PatientControlStatus(PatientControlOutcome.NotApplicable, null);
        }

        var since = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-Math.Max(1, rule.WithinDays));
        var latest = rule.Kind == ControlKind.Vital
            ? await LatestVitalAsync(patientId, rule.Code, since, cancellationToken).ConfigureAwait(false)
            : await LatestLabAsync(patientId, rule.Code, since, cancellationToken).ConfigureAwait(false);

        if (latest is not { } value)
            return new PatientControlStatus(PatientControlOutcome.NoData, null);

        var controlled = rule.Comparator.Matches(value, rule.TargetValue);
        return new PatientControlStatus(
            controlled ? PatientControlOutcome.Controlled : PatientControlOutcome.Uncontrolled, value);
    }

    private async Task<decimal?> LatestVitalAsync(Guid patientId, string loinc, DateTime since, CancellationToken cancellationToken)
    {
        var readings = await _vitals.ListByPatientAsync(patientId, since, cancellationToken).ConfigureAwait(false);
        return readings
            .Where(r => r.ObservationType.Code.Equals(loinc.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.ObservedAtUtc)
            .Select(r => (decimal?)r.Value)
            .FirstOrDefault();
    }

    private async Task<decimal?> LatestLabAsync(Guid patientId, string loinc, DateTime since, CancellationToken cancellationToken)
    {
        var labs = await _labResults.ListByPatientAsync(patientId, since, cancellationToken).ConfigureAwait(false);
        var latest = labs
            .Where(l => l.LoincCode.Equals(loinc.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(l => l.ObservedAtUtc)
            .FirstOrDefault();
        if (latest is null)
            return null;
        return decimal.TryParse(latest.ValueText, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : null;
    }
}
