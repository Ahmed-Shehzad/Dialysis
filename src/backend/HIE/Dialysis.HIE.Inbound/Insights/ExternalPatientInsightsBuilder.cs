using Dialysis.HIE.Inbound.Domain;
using Dialysis.HIE.Inbound.Ports;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace Dialysis.HIE.Inbound.Insights;

/// <summary>
/// Builds a <see cref="PatientInsightsSummary"/> by scanning recent inbound resources, keeping those
/// about the requested patient, and aggregating them by category + source. Deliberately a
/// projection over current state (no rebuild from an event log).
/// </summary>
public sealed class ExternalPatientInsightsBuilder
{
    private static readonly FhirJsonDeserializer _parser =
        new(new DeserializerSettings().UsingMode(DeserializationMode.Recoverable));

    private readonly IReceivedResourceStore _store;
    public ExternalPatientInsightsBuilder(IReceivedResourceStore store) => _store = store;

    public async Task<PatientInsightsSummary> BuildAsync(string patientReference, int scan, int recentTake, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patientReference);
        var rows = await _store.ListRecentAsync(partnerId: null, Math.Clamp(scan, 1, 2000), cancellationToken).ConfigureAwait(false);

        var matched = new List<(ReceivedResource Row, Resource Resource)>();
        foreach (var row in rows)
        {
            Resource resource;
            try { resource = _parser.Deserialize<Resource>(row.FhirJson); }
            catch { continue; }
            if (string.Equals(SubjectId(resource), patientReference, StringComparison.Ordinal))
                matched.Add((row, resource));
        }

        var clinical = matched.Where(m => m.Resource is not Patient).ToList();

        var counts = new InsightsCounts(
            Encounters: clinical.Count(m => m.Resource is Encounter),
            Observations: clinical.Count(m => m.Resource is Observation or DiagnosticReport),
            Documents: clinical.Count(m => m.Resource is DocumentReference),
            Procedures: clinical.Count(m => m.Resource is Procedure),
            Other: clinical.Count(m => m.Resource is not (Encounter or Observation or DiagnosticReport or DocumentReference or Procedure)),
            Total: clinical.Count);

        var recent = clinical
            .Select(m => new InsightsItem(m.Resource.TypeName, ClinicalDate(m.Resource) ?? m.Row.ReceivedAtUtc, m.Row.PartnerId, Display(m.Resource)))
            .OrderByDescending(i => i.Date)
            .Take(Math.Clamp(recentTake, 1, 100))
            .ToList();

        var sources = matched.Select(m => m.Row.PartnerId).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
        var lastUpdated = matched.Count > 0 ? matched.Max(m => m.Row.ReceivedAtUtc) : (DateTime?)null;
        var duplicates = DetectDuplicateTests(clinical);

        return new PatientInsightsSummary(patientReference, sources, lastUpdated, counts, recent, duplicates);
    }

    // Same LOINC observed at >1 source organisation → a duplicate-test alert.
    private static List<DuplicateTestAlert> DetectDuplicateTests(List<(ReceivedResource Row, Resource Resource)> clinical) =>
        clinical
            .Where(m => m.Resource is Observation)
            .Select(m => (Coding: ((Observation)m.Resource).Code?.Coding.FirstOrDefault(), m.Row.PartnerId))
            .Where(x => x.Coding is { Code.Length: > 0 })
            .GroupBy(x => x.Coding!.Code!, StringComparer.Ordinal)
            .Select(g => new
            {
                Code = g.Key,
                Display = g.First().Coding!.Display,
                Sources = g.Select(x => x.PartnerId).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(),
            })
            .Where(g => g.Sources.Count > 1)
            .Select(g => new DuplicateTestAlert(g.Code, g.Display, g.Sources.Count, g.Sources))
            .ToList();

    private static string? SubjectId(Resource resource) => resource switch
    {
        Patient p => p.Id,
        Observation o => RefId(o.Subject),
        Encounter e => RefId(e.Subject),
        Procedure pr => RefId(pr.Subject),
        Condition c => RefId(c.Subject),
        DocumentReference d => RefId(d.Subject),
        DiagnosticReport dr => RefId(dr.Subject),
        _ => null,
    };

    private static string? RefId(ResourceReference? reference) =>
        reference?.Reference?.Split('/').LastOrDefault();

    private static DateTime? ClinicalDate(Resource resource) => resource switch
    {
        Observation o when o.Effective is FhirDateTime fdt && DateTime.TryParse(fdt.Value, out var d) => d,
        Encounter e when e.Period?.StartElement is { } s && DateTime.TryParse(s.Value, out var d) => d,
        Procedure pr when pr.Performed is Period p && p.StartElement is { } s && DateTime.TryParse(s.Value, out var d) => d,
        Procedure pr when pr.Performed is FhirDateTime fdt && DateTime.TryParse(fdt.Value, out var d) => d,
        DocumentReference doc when doc.Date is { } dto => dto.UtcDateTime,
        DiagnosticReport dr when dr.Effective is FhirDateTime fdt && DateTime.TryParse(fdt.Value, out var d) => d,
        _ => null,
    };

    private static string? Display(Resource resource) => resource switch
    {
        Observation o => o.Code?.Coding.FirstOrDefault()?.Display ?? o.Code?.Text,
        Encounter e => e.Class?.Code,
        Procedure pr => pr.Code?.Coding.FirstOrDefault()?.Display ?? pr.Code?.Text,
        DocumentReference d => d.Type?.Coding.FirstOrDefault()?.Display ?? d.Description,
        DiagnosticReport dr => dr.Code?.Coding.FirstOrDefault()?.Display ?? dr.Code?.Text,
        _ => null,
    };
}
