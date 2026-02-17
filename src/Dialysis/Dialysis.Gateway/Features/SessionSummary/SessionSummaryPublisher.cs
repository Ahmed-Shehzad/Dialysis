using Dialysis.Gateway.Features.Fhir;

using global::Hl7.Fhir.Model;

using Microsoft.Extensions.Logging;

namespace Dialysis.Gateway.Features.SessionSummary;

/// <summary>
/// Builds a FHIR R4 transaction bundle for a dialysis session summary.
/// Output: Encounter, Observation set (pre/post weight, BP, UF removed, treatment time, complications), optional Procedure.
/// </summary>
public sealed class SessionSummaryPublisher
{
    private const string LoincSystem = "http://loinc.org";
    private const string UcumSystem = "http://unitsofmeasure.org";
    private const string SnomedSystem = "http://snomed.info/sct";

    // LOINC codes for dialysis session observations
    private const string LoincBodyWeight = "29463-7";           // Body weight
    private const string LoincBloodPressure = "85354-9";       // Blood pressure panel
    private const string LoincUltrafiltration = "99741-1";     // Ultrafiltrate volume removed
    private const string LoincDialysisDuration = "93737-7";    // Hemodialysis duration

    private readonly ILogger<SessionSummaryPublisher> _logger;

    public SessionSummaryPublisher(ILogger<SessionSummaryPublisher> logger) => _logger = logger;

    /// <summary>
    /// Builds a FHIR transaction bundle for the session summary. Can be POSTed to a FHIR endpoint or saved to file.
    /// </summary>
    public Bundle BuildBundle(SessionSummaryInput input, string baseUrl)
    {
        var normalizedBase = baseUrl.TrimEnd('/');
        var baseUrlWithSlash = normalizedBase.EndsWith('/') ? normalizedBase : normalizedBase + "/";

        var bundle = new Bundle { Type = Bundle.BundleType.Transaction };
        var encounterRef = $"{baseUrlWithSlash}Encounter/{input.SessionId}";
        var patientRef = $"{baseUrlWithSlash}Patient/{input.PatientId.Value}";

        // 1. Encounter (dialysis session)
        var encounter = BuildEncounter(input, baseUrlWithSlash);
        bundle.Entry.Add(CreateTransactionEntry(encounterRef, encounter, $"Encounter/{input.SessionId}"));

        // 2. Observations
        var obsIndex = 0;
        if (input.PreWeightKg.HasValue)
        {
            var obs = BuildWeightObservation($"{input.SessionId}-pre-weight", input.PatientId.Value, input.PreWeightKg.Value, input.StartedAt, "Pre-dialysis weight", baseUrlWithSlash);
            bundle.Entry.Add(CreateTransactionEntry($"{baseUrlWithSlash}Observation/{obs.Id}", obs, $"Observation/{obs.Id}"));
            obsIndex++;
        }
        if (input.PostWeightKg.HasValue)
        {
            var obs = BuildWeightObservation($"{input.SessionId}-post-weight", input.PatientId.Value, input.PostWeightKg.Value, input.EndedAt, "Post-dialysis weight", baseUrlWithSlash);
            bundle.Entry.Add(CreateTransactionEntry($"{baseUrlWithSlash}Observation/{obs.Id}", obs, $"Observation/{obs.Id}"));
            obsIndex++;
        }
        if (input.SystolicBp.HasValue || input.DiastolicBp.HasValue)
        {
            var obs = BuildBloodPressureObservation(input, baseUrlWithSlash);
            bundle.Entry.Add(CreateTransactionEntry($"{baseUrlWithSlash}Observation/{obs.Id}", obs, $"Observation/{obs.Id}"));
            obsIndex++;
        }
        if (input.UfRemovedKg.HasValue)
        {
            var obs = BuildObservation($"{input.SessionId}-uf", input.PatientId.Value, LoincUltrafiltration, "Ultrafiltrate volume removed",
                new Quantity(input.UfRemovedKg.Value, "kg", UcumSystem), input.EndedAt, encounterRef, baseUrlWithSlash);
            bundle.Entry.Add(CreateTransactionEntry($"{baseUrlWithSlash}Observation/{obs.Id}", obs, $"Observation/{obs.Id}"));
            obsIndex++;
        }
        if (input.TreatmentMinutes > 0)
        {
            var obs = BuildObservation($"{input.SessionId}-duration", input.PatientId.Value, LoincDialysisDuration, "Hemodialysis duration",
                new Quantity(input.TreatmentMinutes, "min", UcumSystem), input.EndedAt, encounterRef, baseUrlWithSlash);
            bundle.Entry.Add(CreateTransactionEntry($"{baseUrlWithSlash}Observation/{obs.Id}", obs, $"Observation/{obs.Id}"));
            obsIndex++;
        }
        if (!string.IsNullOrWhiteSpace(input.Complications))
        {
            var obs = BuildComplicationsObservation(input, baseUrlWithSlash);
            bundle.Entry.Add(CreateTransactionEntry($"{baseUrlWithSlash}Observation/{obs.Id}", obs, $"Observation/{obs.Id}"));
        }

        // 3. Optional Procedure (hemodialysis performed)
        if (input.IncludeProcedure)
        {
            var procedure = BuildProcedure(input, baseUrlWithSlash);
            var procRef = $"{baseUrlWithSlash}Procedure/{input.SessionId}";
            bundle.Entry.Add(CreateTransactionEntry(procRef, procedure, $"Procedure/{input.SessionId}"));
        }

        _logger.LogDebug("Built session summary bundle for SessionId={SessionId}, Observations={Count}", input.SessionId, obsIndex);

        return bundle;
    }

    /// <summary>
    /// Serializes the bundle to FHIR JSON.
    /// </summary>
    public static string ToJson(Bundle bundle) => FhirMappers.ToFhirJson(bundle);

    /// <summary>
    /// Saves the bundle JSON to a file. Useful for testing or initial development.
    /// </summary>
    public async System.Threading.Tasks.Task SaveToFileAsync(Bundle bundle, string path, CancellationToken cancellationToken = default)
    {
        var json = SessionSummaryPublisher.ToJson(bundle);
        await File.WriteAllTextAsync(path, json, cancellationToken);
        _logger.LogInformation("Session summary bundle saved to {Path}", path);
    }

    private static Encounter BuildEncounter(SessionSummaryInput input, string baseUrl)
    {
        return new Encounter
        {
            Id = input.SessionId,
            Status = Encounter.EncounterStatus.Finished,
            Class = new Coding("http://terminology.hl7.org/CodeSystem/v3-ActCode", "AMB", "ambulatory"),
            Type = [new CodeableConcept(SnomedSystem, "108290001", "Dialysis")],
            Subject = new ResourceReference($"{baseUrl}Patient/{input.PatientId.Value}"),
            Period = new Period
            {
                Start = input.StartedAt.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                End = input.EndedAt.ToString("yyyy-MM-ddTHH:mm:sszzz")
            }
        };
    }

    private static Procedure BuildProcedure(SessionSummaryInput input, string baseUrl)
    {
        var proc = new Procedure
        {
            Id = input.SessionId,
            Status = EventStatus.Completed,
            Code = new CodeableConcept(SnomedSystem, "302497006", "Hemodialysis"),
            Subject = new ResourceReference($"{baseUrl}Patient/{input.PatientId.Value}"),
            Encounter = new ResourceReference($"{baseUrl}Encounter/{input.SessionId}"),
            Performed = new Period
            {
                Start = input.StartedAt.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                End = input.EndedAt.ToString("yyyy-MM-ddTHH:mm:sszzz")
            }
        };
        if (input.UfRemovedKg.HasValue)
            proc.Note = [new Annotation { Text = $"Ultrafiltration removed: {input.UfRemovedKg} kg" }];
        if (!string.IsNullOrWhiteSpace(input.AccessSite))
            proc.BodySite = [new CodeableConcept { Text = input.AccessSite }];
        return proc;
    }

    private static Observation BuildWeightObservation(string id, string patientId, decimal value, DateTimeOffset effective, string display, string baseUrl)
    {
        return BuildObservation(id, patientId, LoincBodyWeight, display,
            new Quantity(value, "kg", UcumSystem), effective, null, baseUrl);
    }

    private static Observation BuildBloodPressureObservation(SessionSummaryInput input, string baseUrl)
    {
        var id = $"{input.SessionId}-bp";
        var obs = new Observation
        {
            Id = id,
            Status = ObservationStatus.Final,
            Code = new CodeableConcept(LoincSystem, LoincBloodPressure, "Blood pressure"),
            Subject = new ResourceReference($"{baseUrl}Patient/{input.PatientId.Value}"),
            Encounter = new ResourceReference($"{baseUrl}Encounter/{input.SessionId}"),
            Effective = new FhirDateTime(input.EndedAt)
        };
        if (input.SystolicBp.HasValue && input.DiastolicBp.HasValue)
        {
            obs.Component =
            [
                new Observation.ComponentComponent { Code = new CodeableConcept(LoincSystem, "8480-6", "Systolic"), Value = new Quantity(input.SystolicBp.Value, "mmHg", UcumSystem) },
                new Observation.ComponentComponent { Code = new CodeableConcept(LoincSystem, "8462-4", "Diastolic"), Value = new Quantity(input.DiastolicBp.Value, "mmHg", UcumSystem) }
            ];
        }
        else if (input.SystolicBp.HasValue)
        {
            obs.Value = new Quantity(input.SystolicBp.Value, "mmHg", UcumSystem);
        }
        return obs;
    }

    private static Observation BuildComplicationsObservation(SessionSummaryInput input, string baseUrl)
    {
        var id = $"{input.SessionId}-complications";
        return new Observation
        {
            Id = id,
            Status = ObservationStatus.Final,
            Code = new CodeableConcept(SnomedSystem, "404684003", "Clinical finding"),
            Subject = new ResourceReference($"{baseUrl}Patient/{input.PatientId.Value}"),
            Encounter = new ResourceReference($"{baseUrl}Encounter/{input.SessionId}"),
            Effective = new FhirDateTime(input.EndedAt),
            Value = new FhirString(input.Complications!)
        };
    }

    private static Observation BuildObservation(string id, string patientId, string loinc, string display, Quantity value, DateTimeOffset effective, string? encounterRef, string baseUrl)
    {
        var obs = new Observation
        {
            Id = id,
            Status = ObservationStatus.Final,
            Code = new CodeableConcept(LoincSystem, loinc, display),
            Subject = new ResourceReference($"{baseUrl}Patient/{patientId}"),
            Effective = new FhirDateTime(effective),
            Value = value
        };
        if (!string.IsNullOrEmpty(encounterRef))
            obs.Encounter = new ResourceReference(encounterRef);
        return obs;
    }

    private static Bundle.EntryComponent CreateTransactionEntry(string fullUrl, Resource resource, string requestUrl)
    {
        return new Bundle.EntryComponent
        {
            FullUrl = fullUrl,
            Resource = resource,
            Request = new Bundle.RequestComponent
            {
                Method = Bundle.HTTPVerb.PUT,
                Url = requestUrl
            }
        };
    }
}
