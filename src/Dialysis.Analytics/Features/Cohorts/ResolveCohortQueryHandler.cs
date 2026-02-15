using Dialysis.ApiClients;
using Dialysis.Analytics.Configuration;
using Dialysis.Analytics.Services;
using Hl7.Fhir.Model;
using Intercessor.Abstractions;
using Microsoft.Extensions.Options;

namespace Dialysis.Analytics.Features.Cohorts;

public sealed class ResolveCohortQueryHandler : IQueryHandler<ResolveCohortQuery, CohortResult>
{
    private const string SystolicBpLoinc = "8480-6,85354-9";

    private readonly IFhirApi _fhirApi;
    private readonly IFhirBundleClient _bundleClient;
    private readonly IAlertingApi _alertingApi;
    private readonly IAnalyticsAuditRecorder _audit;
    private readonly string _fhirBaseUrl;

    public ResolveCohortQueryHandler(
        IFhirApi fhirApi,
        IFhirBundleClient bundleClient,
        IAlertingApi alertingApi,
        IAnalyticsAuditRecorder audit,
        IOptions<AnalyticsOptions> options)
    {
        _fhirApi = fhirApi;
        _bundleClient = bundleClient;
        _alertingApi = alertingApi;
        _audit = audit;
        _fhirBaseUrl = options.Value.FhirBaseUrl.TrimEnd('/') + "/";
    }

    public async Task<CohortResult> HandleAsync(ResolveCohortQuery request, CancellationToken cancellationToken = default)
    {
        var criteria = request.Criteria;
        var (from, to) = ResolveDateRange(criteria.From, criteria.To);

        HashSet<string>? patientIds = null;
        HashSet<string>? encounterIds = null;

        if (criteria.EncounterCount != null)
        {
            var (p, e) = await ResolveEncounterCriterionAsync(from, to, criteria.EncounterCount, cancellationToken);
            patientIds = p;
            encounterIds = e;
        }

        if (criteria.AlertCount != null)
        {
            var alertPatients = await ResolveAlertCriterionAsync(criteria.AlertCount, cancellationToken);
            patientIds = patientIds == null ? alertPatients : [..patientIds.Intersect(alertPatients)];
        }

        if (criteria.ObservationValue != null)
        {
            var (p, e) = await ResolveObservationCriterionAsync(from, to, criteria.ObservationValue, cancellationToken);
            if (patientIds == null)
            {
                patientIds = p;
                encounterIds = e;
            }
            else
            {
                patientIds = new HashSet<string>(patientIds.Intersect(p));
                encounterIds = encounterIds == null ? e : [..encounterIds.Intersect(e)];
            }
        }

        if (patientIds == null)
        {
            var (p, e) = await GetAllEncountersInRangeAsync(from, to, cancellationToken);
            patientIds = p;
            encounterIds = e;
        }

        var result = new CohortResult
        {
            PatientIds = patientIds.ToList(),
            EncounterIds = (encounterIds ?? []).ToList()
        };
        await _audit.RecordAsync("Cohort", "criteria", "resolve", outcome: "0", cancellationToken: cancellationToken);
        return result;
    }

    private static (DateOnly from, DateOnly to) ResolveDateRange(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var f = from ?? today.AddDays(-30);
        var t = to ?? today;
        if (f > t) (f, t) = (t, f);
        return (f, t);
    }

    private async Task<(HashSet<string> patients, HashSet<string> encounters)> ResolveEncounterCriterionAsync(
        DateOnly from, DateOnly to,
        EncounterCountCriterion criterion,
        CancellationToken cancellationToken)
    {
        var fromStr = from.ToString("yyyy-MM-dd");
        var toStr = to.ToString("yyyy-MM-dd");
        var url = $"{_fhirBaseUrl}Encounter?date=ge{fromStr}&date=le{toStr}&_elements=id,subject&_count=500";

        var patients = new Dictionary<string, int>();
        var encounterIds = new HashSet<string>();

        var nextUrl = url;
        while (!string.IsNullOrEmpty(nextUrl))
        {
            var bundle = await _bundleClient.GetBundleAsync(nextUrl, cancellationToken);

            foreach (var entry in bundle.Entry)
            {
                if (entry.Resource is not Encounter enc) continue;
                var patientRef = enc.Subject?.Reference;
                if (string.IsNullOrEmpty(patientRef)) continue;
                var patientId = patientRef.StartsWith("Patient/", StringComparison.OrdinalIgnoreCase)
                    ? patientRef["Patient/".Length..]
                    : patientRef;
                if (string.IsNullOrEmpty(patientId)) continue;

                encounterIds.Add(enc.Id ?? "");
                patients[patientId] = patients.GetValueOrDefault(patientId) + 1;
            }

            nextUrl = bundle.NextLink?.ToString() ?? "";
        }

        var filteredPatients = new HashSet<string>(
            patients.Where(kv => kv.Value >= criterion.MinCount).Select(kv => kv.Key));

        return (filteredPatients, encounterIds);
    }

    private async Task<HashSet<string>> ResolveAlertCriterionAsync(
        AlertCountCriterion criterion,
        CancellationToken cancellationToken)
    {
        var to = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = to.AddDays(-criterion.WithinDays);
        var alerts = await _alertingApi.GetAlertsAsync(cancellationToken);

        var fromDt = from.ToDateTime(TimeOnly.MinValue);
        var toDt = to.ToDateTime(TimeOnly.MaxValue);

        var byPatient = alerts
            .Where(a => a.RaisedAt >= fromDt && a.RaisedAt <= toDt)
            .GroupBy(a => a.PatientId)
            .Where(g => g.Count() >= criterion.MinCount)
            .Select(g => g.Key)
            .ToHashSet();

        return byPatient;
    }

    private async Task<(HashSet<string> patients, HashSet<string> encounters)> ResolveObservationCriterionAsync(
        DateOnly from, DateOnly to,
        ObservationValueCriterion criterion,
        CancellationToken cancellationToken)
    {
        var fromStr = from.ToString("yyyy-MM-dd");
        var toStr = to.ToString("yyyy-MM-dd");
        var url = $"{_fhirBaseUrl}Observation?code={criterion.Code}&date=ge{fromStr}&date=le{toStr}&_elements=subject,encounter,valueQuantity&_count=500";

        var patients = new HashSet<string>();
        var encounters = new HashSet<string>();

        var nextUrl = url;
        while (!string.IsNullOrEmpty(nextUrl))
        {
            var bundle = await _bundleClient.GetBundleAsync(nextUrl, cancellationToken);

            foreach (var entry in bundle.Entry)
            {
                if (entry.Resource is not Observation obs) continue;
                if (obs.Value is not Quantity q || !q.Value.HasValue) continue;

                var matches = criterion.Comparator.ToLowerInvariant() switch
                {
                    "<" => q.Value.Value < (decimal)criterion.Value,
                    "<=" => q.Value.Value <= (decimal)criterion.Value,
                    ">" => q.Value.Value > (decimal)criterion.Value,
                    ">=" => q.Value.Value >= (decimal)criterion.Value,
                    "=" => Math.Abs((double)q.Value.Value - criterion.Value) < 0.01,
                    _ => false
                };

                if (!matches) continue;

                var subjectRef = obs.Subject?.Reference;
                if (!string.IsNullOrEmpty(subjectRef))
                {
                    var pid = subjectRef.StartsWith("Patient/", StringComparison.OrdinalIgnoreCase)
                        ? subjectRef["Patient/".Length..]
                        : subjectRef;
                    if (!string.IsNullOrEmpty(pid)) patients.Add(pid);
                }

                var encRef = obs.Encounter?.Reference;
                if (!string.IsNullOrEmpty(encRef))
                {
                    var eid = encRef.StartsWith("Encounter/", StringComparison.OrdinalIgnoreCase)
                        ? encRef["Encounter/".Length..]
                        : encRef;
                    if (!string.IsNullOrEmpty(eid)) encounters.Add(eid);
                }
            }

            nextUrl = bundle.NextLink?.ToString() ?? "";
        }

        return (patients, encounters);
    }

    private Task<(HashSet<string> patients, HashSet<string> encounters)> GetAllEncountersInRangeAsync(
        DateOnly from, DateOnly to,
        CancellationToken cancellationToken)
    {
        return ResolveEncounterCriterionAsync(from, to, new EncounterCountCriterion { MinCount = 1, WithinDays = 0 }, cancellationToken);
    }
}
