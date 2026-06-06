using Dialysis.CQRS;
using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.Registration.Domain;
using Dialysis.EHR.Registration.Features.SearchPatients;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.QualityMeasures;

/// <summary>Aggregated open-gap count for a single measure across the evaluated cohort.</summary>
public sealed record CohortMeasureGap(string MeasureId, string Title, int PatientsWithGap);

/// <summary>A single patient's open quality gaps within the cohort breakdown.</summary>
public sealed record CohortPatientGaps(
    Guid PatientId,
    string MedicalRecordNumber,
    string Name,
    IReadOnlyList<QualityGap> Gaps);

/// <summary>
/// Population-quality roll-up: how many patients in the cohort carry an open gap for each measure,
/// plus a per-patient breakdown to drive outreach.
/// </summary>
public sealed record CohortQualityResult(
    int PatientsEvaluated,
    int PatientsWithAnyGap,
    IReadOnlyList<CohortMeasureGap> MeasureGaps,
    IReadOnlyList<CohortPatientGaps> PatientBreakdown);

/// <summary>
/// Runs the single-patient <see cref="IQualityMeasureEvaluator"/> across a panel of patients and
/// aggregates the open care gaps — population management built on the same evaluator the chart uses.
/// </summary>
public sealed record EvaluateCohortQualityQuery(int Take = 100)
    : IQuery<CohortQualityResult>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.QualityPopulationRead;
}

public sealed class EvaluateCohortQualityQueryHandler
    : IQueryHandler<EvaluateCohortQualityQuery, CohortQualityResult>
{
    private const int MaxCohort = 500;
    private readonly ICqrsGateway _gateway;
    private readonly IQualityMeasureEvaluator _evaluator;

    public EvaluateCohortQualityQueryHandler(ICqrsGateway gateway, IQualityMeasureEvaluator evaluator)
    {
        _gateway = gateway;
        _evaluator = evaluator;
    }

    public async Task<CohortQualityResult> HandleAsync(
        EvaluateCohortQualityQuery request, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, MaxCohort);

        // Resolve the cohort through the existing patient-search read model (active patients only).
        var cohort = await _gateway.SendQueryAsync<SearchPatientsQuery, PatientSearchResult>(
            new SearchPatientsQuery(Status: PatientStatus.Active, Take: take),
            cancellationToken).ConfigureAwait(false);

        var measureGapCounts = new Dictionary<string, (string Title, int Count)>(StringComparer.OrdinalIgnoreCase);
        var breakdown = new List<CohortPatientGaps>();
        var patientsWithAnyGap = 0;

        foreach (var patient in cohort.Items)
        {
            var gaps = await _evaluator.EvaluateAsync(patient.Id, cancellationToken).ConfigureAwait(false);
            if (gaps.Count == 0)
                continue;

            patientsWithAnyGap++;
            breakdown.Add(new CohortPatientGaps(
                patient.Id,
                patient.MedicalRecordNumber,
                $"{patient.FamilyName}, {patient.GivenName}",
                gaps));

            foreach (var gap in gaps)
            {
                var current = measureGapCounts.TryGetValue(gap.MeasureId, out var existing)
                    ? existing
                    : (Title: gap.Title, Count: 0);
                measureGapCounts[gap.MeasureId] = (current.Title, current.Count + 1);
            }
        }

        var measureGaps = measureGapCounts
            .Select(kvp => new CohortMeasureGap(kvp.Key, kvp.Value.Title, kvp.Value.Count))
            .OrderByDescending(m => m.PatientsWithGap)
            .ThenBy(m => m.MeasureId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new CohortQualityResult(cohort.Items.Count, patientsWithAnyGap, measureGaps, breakdown);
    }
}
