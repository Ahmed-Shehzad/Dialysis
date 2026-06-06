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
            Medications: clinical.Count(m => IsMedication(m.Resource)),
            Allergies: clinical.Count(m => m.Resource is AllergyIntolerance),
            Problems: clinical.Count(m => m.Resource is Condition),
            Other: clinical.Count(m => !IsKnownCategory(m.Resource)),
            Total: clinical.Count);

        var recent = ProjectList(clinical, _ => true)
            .Take(Math.Clamp(recentTake, 1, 100))
            .ToList();
        var medications = ProjectList(clinical, IsMedication);
        var allergies = ProjectList(clinical, r => r is AllergyIntolerance);
        var problems = ProjectList(clinical, r => r is Condition);

        var sources = matched.Select(m => m.Row.PartnerId).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
        var lastUpdated = matched.Count > 0 ? matched.Max(m => m.Row.ReceivedAtUtc) : (DateTime?)null;

        return new PatientInsightsSummary(
            patientReference,
            sources,
            lastUpdated,
            counts,
            recent,
            medications,
            allergies,
            problems,
            DetectDuplicateTests(clinical),
            DetectDuplicateMedications(clinical),
            DetectAllergyConflicts(clinical));
    }

    private static List<InsightsItem> ProjectList(
        List<(ReceivedResource Row, Resource Resource)> clinical, Func<Resource, bool> predicate) =>
        clinical
            .Where(m => predicate(m.Resource))
            .Select(m => new InsightsItem(m.Resource.TypeName, ClinicalDate(m.Resource) ?? m.Row.ReceivedAtUtc, m.Row.PartnerId, Display(m.Resource)))
            .OrderByDescending(i => i.Date)
            .ToList();

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
                Sources = SourcesOf(g.Select(x => x.PartnerId)),
            })
            .Where(g => g.Sources.Count > 1)
            .Select(g => new DuplicateTestAlert(g.Code, g.Display, g.Sources.Count, g.Sources))
            .ToList();

    // Same medication code reported by >1 source → a reconciliation signal.
    private static List<DuplicateMedicationAlert> DetectDuplicateMedications(List<(ReceivedResource Row, Resource Resource)> clinical) =>
        clinical
            .Where(m => IsMedication(m.Resource))
            .Select(m => (Coding: MedicationConcept(m.Resource)?.Coding.FirstOrDefault(), m.Row.PartnerId))
            .Where(x => x.Coding is { Code.Length: > 0 })
            .GroupBy(x => x.Coding!.Code!, StringComparer.Ordinal)
            .Select(g => new
            {
                Code = g.Key,
                Display = g.First().Coding!.Display,
                Sources = SourcesOf(g.Select(x => x.PartnerId)),
            })
            .Where(g => g.Sources.Count > 1)
            .Select(g => new DuplicateMedicationAlert(g.Code, g.Display, g.Sources.Count, g.Sources))
            .ToList();

    // An external medication that matches a recorded allergy → a safety signal. Deterministic match:
    // a shared coding code, or one display containing the other (e.g. allergy "Penicillin" vs med
    // "Penicillin V"). No external drug-allergy knowledge base.
    private static List<AllergyConflictAlert> DetectAllergyConflicts(List<(ReceivedResource Row, Resource Resource)> clinical)
    {
        var allergies = clinical
            .Where(m => m.Resource is AllergyIntolerance)
            .Select(m => (Concept: ((AllergyIntolerance)m.Resource).Code, m.Row.PartnerId))
            .Where(x => x.Concept is not null)
            .ToList();
        var medications = clinical
            .Where(m => IsMedication(m.Resource))
            .Select(m => (Concept: MedicationConcept(m.Resource), m.Row.PartnerId))
            .Where(x => x.Concept is not null)
            .ToList();

        var alerts = new List<AllergyConflictAlert>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var allergy in allergies)
        {
            foreach (var med in medications)
            {
                if (!ConceptsMatch(allergy.Concept!, med.Concept!))
                    continue;
                var medDisplay = ConceptDisplay(med.Concept!);
                var allergyDisplay = ConceptDisplay(allergy.Concept!);
                if (!seen.Add($"{medDisplay}|{allergyDisplay}"))
                    continue;
                alerts.Add(new AllergyConflictAlert(medDisplay, allergyDisplay, SourcesOf([allergy.PartnerId, med.PartnerId])));
            }
        }
        return alerts;
    }

    private static bool ConceptsMatch(CodeableConcept a, CodeableConcept b)
    {
        var bCodes = b.Coding.Where(c => !string.IsNullOrWhiteSpace(c.Code)).Select(c => c.Code!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (a.Coding.Any(c => !string.IsNullOrWhiteSpace(c.Code) && bCodes.Contains(c.Code!)))
            return true;
        var ad = ConceptDisplay(a);
        var bd = ConceptDisplay(b);
        if (string.IsNullOrWhiteSpace(ad) || string.IsNullOrWhiteSpace(bd))
            return false;
        return ad.Contains(bd, StringComparison.OrdinalIgnoreCase) || bd.Contains(ad, StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> SourcesOf(IEnumerable<string> partnerIds) =>
        partnerIds.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();

    private static bool IsMedication(Resource resource) => resource is MedicationStatement or MedicationRequest;

    private static bool IsKnownCategory(Resource resource) =>
        resource is Encounter or Observation or DiagnosticReport or DocumentReference or Procedure
            or MedicationStatement or MedicationRequest or AllergyIntolerance or Condition;

    private static CodeableConcept? MedicationConcept(Resource resource) => resource switch
    {
        MedicationStatement ms => ms.Medication as CodeableConcept,
        MedicationRequest mr => mr.Medication as CodeableConcept,
        _ => null,
    };

    private static string ConceptDisplay(CodeableConcept concept) =>
        concept.Coding.FirstOrDefault()?.Display ?? concept.Text ?? concept.Coding.FirstOrDefault()?.Code ?? "";

    private static string? SubjectId(Resource resource) => resource switch
    {
        Patient p => p.Id,
        Observation o => RefId(o.Subject),
        Encounter e => RefId(e.Subject),
        Procedure pr => RefId(pr.Subject),
        Condition c => RefId(c.Subject),
        DocumentReference d => RefId(d.Subject),
        DiagnosticReport dr => RefId(dr.Subject),
        MedicationStatement ms => RefId(ms.Subject),
        MedicationRequest mr => RefId(mr.Subject),
        AllergyIntolerance a => RefId(a.Patient),
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
        MedicationStatement ms when ms.Effective is FhirDateTime fdt && DateTime.TryParse(fdt.Value, out var d) => d,
        Condition c when c.RecordedDate is { } rd && DateTime.TryParse(rd, out var d) => d,
        _ => null,
    };

    private static string? Display(Resource resource) => resource switch
    {
        Observation o => o.Code?.Coding.FirstOrDefault()?.Display ?? o.Code?.Text,
        Encounter e => e.Class?.Code,
        Procedure pr => pr.Code?.Coding.FirstOrDefault()?.Display ?? pr.Code?.Text,
        DocumentReference d => d.Type?.Coding.FirstOrDefault()?.Display ?? d.Description,
        DiagnosticReport dr => dr.Code?.Coding.FirstOrDefault()?.Display ?? dr.Code?.Text,
        MedicationStatement or MedicationRequest => MedicationConcept(resource) is { } mc ? ConceptDisplay(mc) : null,
        AllergyIntolerance a => a.Code is { } ac ? ConceptDisplay(ac) : null,
        Condition c => c.Code?.Coding.FirstOrDefault()?.Display ?? c.Code?.Text,
        _ => null,
    };
}
