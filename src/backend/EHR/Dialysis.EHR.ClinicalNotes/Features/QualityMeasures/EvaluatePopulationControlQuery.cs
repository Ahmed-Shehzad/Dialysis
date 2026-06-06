using Dialysis.CQRS;
using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.Registration.Domain;
using Dialysis.EHR.Registration.Features.SearchPatients;
using Dialysis.Module.Contracts.Authorization;
using Microsoft.Extensions.Options;

namespace Dialysis.EHR.ClinicalNotes.Features.QualityMeasures;

/// <summary>One patient's control standing within the population breakdown.</summary>
public sealed record PatientControlBreakdown(
    Guid PatientId,
    string MedicalRecordNumber,
    string Name,
    string Outcome,
    decimal? Value);

/// <summary>Population control roll-up for a single measure across the active cohort.</summary>
public sealed record PopulationControlResult(
    string MeasureId,
    string Title,
    int InCohort,
    int Controlled,
    int Uncontrolled,
    int NoData,
    double ControlRatePercent,
    IReadOnlyList<PatientControlBreakdown> Breakdown);

/// <summary>
/// Computes how much of the active condition cohort has its condition controlled for a configured
/// measure (e.g. % of hypertensives with BP &lt; target) — population management built on the same
/// per-patient evaluator, mirroring <see cref="EvaluateCohortQualityQuery"/>.
/// </summary>
public sealed record EvaluatePopulationControlQuery(string MeasureId, int Take = 100)
    : IQuery<PopulationControlResult>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.QualityPopulationRead;
}

public sealed class EvaluatePopulationControlQueryHandler
    : IQueryHandler<EvaluatePopulationControlQuery, PopulationControlResult>
{
    private const int MaxCohort = 500;
    private readonly ICqrsGateway _gateway;
    private readonly IConditionControlEvaluator _evaluator;
    private readonly ControlMeasureOptions _options;

    public EvaluatePopulationControlQueryHandler(
        ICqrsGateway gateway, IConditionControlEvaluator evaluator, IOptions<ControlMeasureOptions> options)
    {
        _gateway = gateway;
        _evaluator = evaluator;
        _options = options.Value;
    }

    public async Task<PopulationControlResult> HandleAsync(
        EvaluatePopulationControlQuery request, CancellationToken cancellationToken)
    {
        var rule = _options.Measures.FirstOrDefault(m => m.Id.Equals(request.MeasureId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Control measure '{request.MeasureId}' is not configured.");

        var take = Math.Clamp(request.Take, 1, MaxCohort);
        var cohort = await _gateway.SendQueryAsync<SearchPatientsQuery, PatientSearchResult>(
            new SearchPatientsQuery(Status: PatientStatus.Active, Take: take), cancellationToken).ConfigureAwait(false);

        int controlled = 0, uncontrolled = 0, noData = 0;
        var breakdown = new List<PatientControlBreakdown>();

        foreach (var patient in cohort.Items)
        {
            var status = await _evaluator.EvaluateAsync(patient.Id, rule, cancellationToken).ConfigureAwait(false);
            if (status.Outcome == PatientControlOutcome.NotApplicable)
                continue;

            switch (status.Outcome)
            {
                case PatientControlOutcome.Controlled: controlled++; break;
                case PatientControlOutcome.Uncontrolled: uncontrolled++; break;
                default: noData++; break;
            }

            breakdown.Add(new PatientControlBreakdown(
                patient.Id, patient.MedicalRecordNumber, $"{patient.FamilyName}, {patient.GivenName}",
                status.Outcome.ToString(), status.Value));
        }

        var inCohort = controlled + uncontrolled + noData;
        var measured = controlled + uncontrolled;
        var rate = measured == 0 ? 0d : Math.Round(controlled * 100d / measured, 1);

        return new PopulationControlResult(
            rule.Id, rule.Title, inCohort, controlled, uncontrolled, noData, rate,
            [.. breakdown.OrderBy(b => b.Outcome, StringComparer.Ordinal).ThenBy(b => b.Name, StringComparer.OrdinalIgnoreCase)]);
    }
}
