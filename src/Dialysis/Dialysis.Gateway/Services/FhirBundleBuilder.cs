using Dialysis.Gateway.Features.Fhir;

namespace Dialysis.Gateway.Services;

/// <summary>
/// Builds FHIR R4 bundles from patient data. Uses FhirMappers for domain→FHIR mapping.
/// </summary>
public sealed class FhirBundleBuilder : IFhirBundleBuilder
{
    public string BuildPatientEverythingBundle(PatientDataAggregate data, string baseUrl)
    {
        var bundle = new global::Hl7.Fhir.Model.Bundle
        {
            Type = Hl7.Fhir.Model.Bundle.BundleType.Collection,
            Total = 1 + data.Conditions.Count + data.Episodes.Count + data.Sessions.Count * 2 + data.Observations.Count + data.MedicationAdministrations.Count
        };

        bundle.Entry.Add(CreateEntry($"{baseUrl}Patient/{data.Patient.LogicalId.Value}", FhirMappers.ToFhirPatient(data.Patient, baseUrl)));

        foreach (var c in data.Conditions)
            bundle.Entry.Add(CreateEntry($"{baseUrl}Condition/{c.Id}", FhirMappers.ToFhirCondition(c, baseUrl)));
        foreach (var e in data.Episodes)
            bundle.Entry.Add(CreateEntry($"{baseUrl}EpisodeOfCare/{e.Id}", FhirMappers.ToFhirEpisodeOfCare(e, baseUrl)));
        foreach (var s in data.Sessions)
            bundle.Entry.Add(CreateEntry($"{baseUrl}Encounter/{s.Id}", FhirMappers.ToFhirEncounter(s, baseUrl)));
        foreach (var o in data.Observations)
            bundle.Entry.Add(CreateEntry($"{baseUrl}Observation/{o.Id}", FhirMappers.ToFhirObservation(o, baseUrl)));
        foreach (var s in data.Sessions)
            bundle.Entry.Add(CreateEntry($"{baseUrl}Procedure/{s.Id}", FhirMappers.ToFhirProcedure(s, baseUrl)));
        foreach (var m in data.MedicationAdministrations)
            bundle.Entry.Add(CreateEntry($"{baseUrl}MedicationAdministration/{m.Id}", FhirMappers.ToFhirMedicationAdministration(m, baseUrl)));

        return FhirMappers.ToFhirJson(bundle);
    }

    public string BuildEhrPushTransactionBundle(PatientDataAggregate data, string baseUrl)
    {
        var bundle = new global::Hl7.Fhir.Model.Bundle
        {
            Type = Hl7.Fhir.Model.Bundle.BundleType.Transaction
        };

        bundle.Entry.Add(CreateTransactionEntry($"{baseUrl}Patient/{data.Patient.LogicalId.Value}", FhirMappers.ToFhirPatient(data.Patient, baseUrl), $"Patient/{data.Patient.LogicalId.Value}"));
        foreach (var c in data.Conditions)
            bundle.Entry.Add(CreateTransactionEntry($"{baseUrl}Condition/{c.Id}", FhirMappers.ToFhirCondition(c, baseUrl), $"Condition/{c.Id}"));
        foreach (var e in data.Episodes)
            bundle.Entry.Add(CreateTransactionEntry($"{baseUrl}EpisodeOfCare/{e.Id}", FhirMappers.ToFhirEpisodeOfCare(e, baseUrl), $"EpisodeOfCare/{e.Id}"));
        foreach (var s in data.Sessions)
            bundle.Entry.Add(CreateTransactionEntry($"{baseUrl}Encounter/{s.Id}", FhirMappers.ToFhirEncounter(s, baseUrl), $"Encounter/{s.Id}"));
        foreach (var o in data.Observations)
            bundle.Entry.Add(CreateTransactionEntry($"{baseUrl}Observation/{o.Id}", FhirMappers.ToFhirObservation(o, baseUrl), $"Observation/{o.Id}"));
        foreach (var s in data.Sessions)
            bundle.Entry.Add(CreateTransactionEntry($"{baseUrl}Procedure/{s.Id}", FhirMappers.ToFhirProcedure(s, baseUrl), $"Procedure/{s.Id}"));
        foreach (var m in data.MedicationAdministrations)
            bundle.Entry.Add(CreateTransactionEntry($"{baseUrl}MedicationAdministration/{m.Id}", FhirMappers.ToFhirMedicationAdministration(m, baseUrl), $"MedicationAdministration/{m.Id}"));

        return FhirMappers.ToFhirJson(bundle);
    }

    private static global::Hl7.Fhir.Model.Bundle.EntryComponent CreateEntry(string fullUrl, global::Hl7.Fhir.Model.Resource resource) =>
        new() { FullUrl = fullUrl, Resource = resource };

    private static global::Hl7.Fhir.Model.Bundle.EntryComponent CreateTransactionEntry(string fullUrl, global::Hl7.Fhir.Model.Resource resource, string url) =>
        new()
        {
            FullUrl = fullUrl,
            Resource = resource,
            Request = new global::Hl7.Fhir.Model.Bundle.RequestComponent
            {
                Method = Hl7.Fhir.Model.Bundle.HTTPVerb.PUT,
                Url = url
            }
        };
}
