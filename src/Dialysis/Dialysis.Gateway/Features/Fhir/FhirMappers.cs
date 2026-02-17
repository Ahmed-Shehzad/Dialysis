using System.Text.Json;

using Dialysis.Domain.Aggregates;
using Dialysis.Domain.Entities;

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

    public static global::Hl7.Fhir.Model.Patient ToFhirPatient(Dialysis.Domain.Entities.Patient patient, string baseUrl)
    {
        var fhir = new global::Hl7.Fhir.Model.Patient
        {
            Id = patient.LogicalId.Value,
            Identifier =
            [
                new global::Hl7.Fhir.Model.Identifier("urn:dialysis:patient", patient.LogicalId.Value)
            ],
            Active = true
        };

        if (patient.FamilyName is not null || patient.GivenNames is not null)
        {
            var name = new global::Hl7.Fhir.Model.HumanName
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

    public static global::Hl7.Fhir.Model.Observation ToFhirObservation(Dialysis.Domain.Aggregates.Observation observation, string baseUrl)
    {
        var fhir = new global::Hl7.Fhir.Model.Observation
        {
            Id = observation.Id.ToString(),
            Status = global::Hl7.Fhir.Model.ObservationStatus.Final,
            Code = new global::Hl7.Fhir.Model.CodeableConcept(LoincSystem, observation.LoincCode.Value, observation.Display ?? observation.LoincCode.Value),
            Subject = new global::Hl7.Fhir.Model.ResourceReference($"{baseUrl}Patient/{observation.PatientId.Value}"),
            Effective = new global::Hl7.Fhir.Model.FhirDateTime(observation.Effective.Value)
        };

        if (observation.NumericValue.HasValue && observation.Unit is not null)
        {
            fhir.Value = new global::Hl7.Fhir.Model.Quantity(observation.NumericValue.Value, observation.Unit.Value, UcumSystem);
        }

        return fhir;
    }

    public static global::Hl7.Fhir.Model.Bundle ToFhirSearchBundle(IReadOnlyList<global::Hl7.Fhir.Model.Observation> observations, string baseUrl)
    {
        var bundle = new global::Hl7.Fhir.Model.Bundle
        {
            Type = global::Hl7.Fhir.Model.Bundle.BundleType.Searchset,
            Total = observations.Count
        };

        foreach (var obs in observations)
        {
            bundle.Entry.Add(new global::Hl7.Fhir.Model.Bundle.EntryComponent
            {
                FullUrl = $"{baseUrl}Observation/{obs.Id}",
                Resource = obs
            });
        }

        return bundle;
    }

    private static readonly JsonSerializerOptions FhirJsonOptions =
        new JsonSerializerOptions().ForFhir(global::Hl7.Fhir.Model.ModelInfo.ModelInspector).Pretty();

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
        var fhir = JsonSerializer.Deserialize<global::Hl7.Fhir.Model.Patient>(json, FhirJsonOptions)
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
