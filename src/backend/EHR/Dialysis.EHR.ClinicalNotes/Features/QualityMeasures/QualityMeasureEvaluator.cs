using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.PatientChart.Ports;
using Microsoft.Extensions.Options;

namespace Dialysis.EHR.ClinicalNotes.Features.QualityMeasures;

/// <summary>An open quality-measure care gap surfaced at the point of care.</summary>
public sealed record QualityGap(string MeasureId, string Title, string Detail);

/// <summary>
/// Evaluates configured quality / MIPS measures against the patient's chart and returns open care gaps,
/// so providers document the data the measure needs. Deterministic and config-driven (empty → no gaps).
/// </summary>
public interface IQualityMeasureEvaluator
{
    Task<IReadOnlyList<QualityGap>> EvaluateAsync(Guid patientId, CancellationToken cancellationToken = default);
}

public sealed class QualityMeasureEvaluator : IQualityMeasureEvaluator
{
    private readonly IProblemListRepository _problems;
    private readonly ILabResultRepository _labResults;
    private readonly TimeProvider _timeProvider;
    private readonly QualityMeasureOptions _options;

    public QualityMeasureEvaluator(IProblemListRepository problems,
        ILabResultRepository labResults,
        TimeProvider timeProvider,
        IOptions<QualityMeasureOptions> options)
    {
        _problems = problems;
        _labResults = labResults;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<QualityGap>> EvaluateAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        if (_options.Measures.Count == 0)
            return [];

        var problems = await _problems.ListByPatientAsync(patientId, false, cancellationToken).ConfigureAwait(false);
        var problemCodes = problems.Select(p => p.Condition.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var gaps = new List<QualityGap>();
        foreach (var measure in _options.Measures)
        {
            if (string.IsNullOrWhiteSpace(measure.ExpectedLoinc))
                continue;
            // A measure with no condition filter applies to everyone; otherwise the patient must carry
            // one of the listed problems for it to apply.
            if (measure.AppliesToAnyIcd10.Count > 0
                && !measure.AppliesToAnyIcd10.Exists(c => problemCodes.Contains(c.Trim())))
                continue;

            var months = Math.Max(1, measure.WithinMonths);
            var since = _timeProvider.GetUtcNow().UtcDateTime.AddMonths(-months);
            var labs = await _labResults.ListByPatientAsync(patientId, since, cancellationToken).ConfigureAwait(false);
            var satisfied = labs.Any(l => l.LoincCode.Equals(measure.ExpectedLoinc.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!satisfied)
            {
                gaps.Add(new QualityGap(
                    measure.Id,
                    measure.Title,
                    $"No result for {measure.ExpectedLoinc} in the last {months} months."));
            }
        }
        return gaps;
    }
}
