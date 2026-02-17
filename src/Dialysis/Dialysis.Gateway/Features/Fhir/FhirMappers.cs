using System.Text.Json;
using global::Hl7.Fhir.Model;
using global::Hl7.Fhir.Serialization;

namespace Dialysis.Gateway.Features.Fhir;

/// <summary>
/// Maps between domain entities and FHIR R4 resources.
/// </summary>
public static class FhirMappers
{
    private const string LoincSystem = "http://loinc.org";
    private const string UcumSystem = "http://unitsofmeasure.org";
    private const string SnomedSystem = "http://snomed.info/sct";

    public static Patient ToFhirPatient(Dialysis.Domain.Entities.Patient patient, string baseUrl)
    {
        var fhir = new Patient
        {
            Id = patient.LogicalId.Value,
            Identifier =
            [
                new Identifier("urn:dialysis:patient", patient.LogicalId.Value)
            ],
            Active = true
        };

        if (patient.FamilyName is not null || patient.GivenNames is not null)
        {
            var name = new HumanName
            {
                Family = patient.FamilyName,
                Given = patient.GivenNames is not null ? [patient.GivenNames] : []
            };
            fhir.Name = [name];
        }

        if (patient.BirthDate.HasValue)
            fhir.BirthDate = patient.BirthDate.Value.ToString("yyyy-MM-dd");

        return fhir;
    }

    public static Observation ToFhirObservation(Dialysis.Domain.Aggregates.Observation observation, string baseUrl)
    {
        var fhir = new Observation
        {
            Id = observation.Id.ToString(),
            Status = ObservationStatus.Final,
            Code = new CodeableConcept(LoincSystem, observation.LoincCode.Value, observation.Display ?? observation.LoincCode.Value),
            Subject = new ResourceReference($"{baseUrl}Patient/{observation.PatientId.Value}"),
            Effective = new FhirDateTime(observation.Effective.Value)
        };

        if (observation.NumericValue.HasValue && observation.Unit is not null)
        {
            fhir.Value = new Quantity(observation.NumericValue.Value, observation.Unit.Value, UcumSystem);
        }

        return fhir;
    }

    public static Bundle ToFhirSearchBundle(IReadOnlyList<Observation> observations, string baseUrl)
    {
        var bundle = new Bundle
        {
            Type = Bundle.BundleType.Searchset,
            Total = observations.Count
        };

        foreach (var obs in observations)
        {
            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"{baseUrl}Observation/{obs.Id}",
                Resource = obs
            });
        }

        return bundle;
    }

    /// <summary>Map dialysis Session to FHIR Encounter (dialysis visit). 1 Session = 1 Encounter.</summary>
    public static Encounter ToFhirEncounter(Dialysis.Domain.Aggregates.Session session, string baseUrl)
    {
        var status = session.Status == Domain.Aggregates.SessionStatus.Completed
            ? Encounter.EncounterStatus.Finished
            : Encounter.EncounterStatus.InProgress;

        var fhir = new Encounter
        {
            Id = session.Id.ToString(),
            Status = status,
            Class = new Coding("http://terminology.hl7.org/CodeSystem/v3-ActCode", "AMB", "ambulatory"),
            Type =
            [
                new CodeableConcept(SnomedSystem, "108290001", "Dialysis")
            ],
            Subject = new ResourceReference($"{baseUrl}Patient/{session.PatientId.Value}"),
            Period = new Period
            {
                Start = session.StartedAt.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                End = session.EndedAt?.ToString("yyyy-MM-ddTHH:mm:sszzz")
            }
        };
        return fhir;
    }

    /// <summary>Map dialysis Session to FHIR Procedure (hemodialysis).</summary>
    public static Procedure ToFhirProcedure(Dialysis.Domain.Aggregates.Session session, string baseUrl)
    {
        var fhir = new Procedure
        {
            Id = session.Id.ToString(),
            Status = session.Status == Domain.Aggregates.SessionStatus.Completed ? EventStatus.Completed : EventStatus.InProgress,
            Code = new CodeableConcept(SnomedSystem, "302497006", "Hemodialysis"),
            Subject = new ResourceReference($"{baseUrl}Patient/{session.PatientId.Value}"),
            Encounter = new ResourceReference($"{baseUrl}Encounter/{session.Id}")
        };

        fhir.Performed = new Period
        {
            Start = session.StartedAt.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            End = session.EndedAt?.ToString("yyyy-MM-ddTHH:mm:sszzz")
        };

        if (session.UfRemovedKg.HasValue)
        {
            fhir.Note =
            [
                new Annotation
                {
                    Text = $"Ultrafiltration removed: {session.UfRemovedKg} kg"
                }
            ];
        }

        if (!string.IsNullOrWhiteSpace(session.AccessSite))
        {
            fhir.BodySite =
            [
                new CodeableConcept
                {
                    Text = session.AccessSite
                }
            ];
        }

        return fhir;
    }

    public static Bundle ToFhirProcedureSearchBundle(IReadOnlyList<Procedure> procedures, string baseUrl)
    {
        var bundle = new Bundle
        {
            Type = Bundle.BundleType.Searchset,
            Total = procedures.Count
        };

        foreach (var proc in procedures)
        {
            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"{baseUrl}Procedure/{proc.Id}",
                Resource = proc
            });
        }

        return bundle;
    }

    /// <summary>Map domain Condition to FHIR Condition.</summary>
    public static global::Hl7.Fhir.Model.Condition ToFhirCondition(Dialysis.Domain.Entities.Condition condition, string baseUrl)
    {
        var fhir = new global::Hl7.Fhir.Model.Condition
        {
            Id = condition.Id.ToString(),
            ClinicalStatus = new CodeableConcept("http://terminology.hl7.org/CodeSystem/condition-clinical", condition.ClinicalStatus),
            VerificationStatus = new CodeableConcept("http://terminology.hl7.org/CodeSystem/condition-ver-status", condition.VerificationStatus),
            Code = new CodeableConcept(condition.CodeSystem, condition.Code, condition.Display),
            Subject = new ResourceReference($"{baseUrl}Patient/{condition.PatientId.Value}")
        };
        if (condition.OnsetDateTime.HasValue)
            fhir.Onset = new FhirDateTime(condition.OnsetDateTime.Value);
        if (condition.RecordedDate.HasValue)
            fhir.RecordedDate = condition.RecordedDate.Value.ToString("yyyy-MM-dd");
        return fhir;
    }

    /// <summary>Map domain EpisodeOfCare to FHIR EpisodeOfCare.</summary>
    public static global::Hl7.Fhir.Model.EpisodeOfCare ToFhirEpisodeOfCare(Dialysis.Domain.Entities.EpisodeOfCare episode, string baseUrl)
    {
        var fhir = new global::Hl7.Fhir.Model.EpisodeOfCare
        {
            Id = episode.Id.ToString(),
            Status = Enum.TryParse<global::Hl7.Fhir.Model.EpisodeOfCare.EpisodeOfCareStatus>(episode.Status, ignoreCase: true, out var statusEnum)
                ? statusEnum
                : global::Hl7.Fhir.Model.EpisodeOfCare.EpisodeOfCareStatus.Active,
            Patient = new ResourceReference($"{baseUrl}Patient/{episode.PatientId.Value}")
        };
        if (!string.IsNullOrEmpty(episode.Description))
            fhir.Type = [new CodeableConcept { Text = episode.Description }];
        if (episode.PeriodStart.HasValue || episode.PeriodEnd.HasValue)
        {
            fhir.Period = new Period
            {
                Start = episode.PeriodStart?.ToString("yyyy-MM-dd"),
                End = episode.PeriodEnd?.ToString("yyyy-MM-dd")
            };
        }
        foreach (var condId in episode.DiagnosisConditionIds)
        {
            fhir.Diagnosis.Add(new global::Hl7.Fhir.Model.EpisodeOfCare.DiagnosisComponent
            {
                Condition = new ResourceReference($"{baseUrl}Condition/{condId}")
            });
        }
        return fhir;
    }

    private static readonly JsonSerializerOptions FhirJsonOptions =
        new JsonSerializerOptions().ForFhir(ModelInfo.ModelInspector).Pretty();

    /// <summary>
    /// Serialize a FHIR resource to JSON using Firely's serializer (FHIR-compliant format).
    /// </summary>
    public static string ToFhirJson(Base resource)
    {
        return JsonSerializer.Serialize(resource, resource.GetType(), FhirJsonOptions);
    }

    /// <summary>
    /// Parse FHIR Patient JSON and extract logicalId, familyName, givenNames, birthDate.
    /// Uses first identifier with system "urn:dialysis:patient", or first identifier, or "id" property.
    /// </summary>
    public static (string LogicalId, string? FamilyName, string? GivenNames, DateTime? BirthDate) FromFhirPatientJson(string json)
    {
        var fhir = JsonSerializer.Deserialize<Patient>(json, FhirJsonOptions)
            ?? throw new ArgumentException("Invalid FHIR Patient JSON.");

        var logicalId = fhir.Id
            ?? fhir.Identifier?.FirstOrDefault(i => i.System == "urn:dialysis:patient")?.Value
            ?? fhir.Identifier?.FirstOrDefault()?.Value
            ?? throw new ArgumentException("FHIR Patient must have an identifier or id.");

        var familyName = fhir.Name?.FirstOrDefault()?.Family;
        var givenNames = fhir.Name?.FirstOrDefault()?.Given?.FirstOrDefault();
        var birthDate = fhir.BirthDate is not null && DateTime.TryParse(fhir.BirthDate, out var bd)
            ? (DateTime?)bd
            : null;

        return (logicalId, familyName, givenNames, birthDate);
    }
}
