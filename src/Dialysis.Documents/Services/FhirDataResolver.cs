using Dialysis.ApiClients;
using Hl7.Fhir.Model;

namespace Dialysis.Documents.Services;

/// <summary>Resolves FHIR Patient and Encounter to field name/value pairs for PDF form filling.</summary>
public sealed class FhirDataResolver : IFhirDataResolver
{
    private readonly IFhirApi _fhirApi;

    public FhirDataResolver(IFhirApi fhirApi)
    {
        _fhirApi = fhirApi;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetPatientFieldValuesAsync(string patientId, CancellationToken cancellationToken = default)
    {
        var patient = await _fhirApi.GetPatient(patientId, cancellationToken);
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["PatientId"] = patient.Id ?? "",
            ["PatientName"] = FormatPatientName(patient),
            ["PatientFirstName"] = patient.Name?.FirstOrDefault()?.Given?.FirstOrDefault() ?? "",
            ["PatientLastName"] = patient.Name?.FirstOrDefault()?.Family ?? "",
            ["PatientDOB"] = patient.BirthDate ?? "",
            ["PatientGender"] = patient.Gender?.ToString() ?? ""
        };
        if (patient.Identifier?.Any() == true)
            dict["PatientIdentifier"] = string.Join(", ", patient.Identifier.Select(i => i.Value));
        return dict;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetEncounterFieldValuesAsync(string encounterId, CancellationToken cancellationToken = default)
    {
        var encounter = await _fhirApi.GetEncounter(encounterId, cancellationToken);
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["EncounterId"] = encounter.Id ?? "",
            ["EncounterStart"] = encounter.Period?.Start ?? "",
            ["EncounterEnd"] = encounter.Period?.End ?? "",
            ["SessionDate"] = encounter.Period?.Start?.Split('T')[0] ?? "",
            ["EncounterStatus"] = encounter.Status?.ToString() ?? ""
        };
        return dict;
    }

    private static string FormatPatientName(Patient patient)
    {
        if (patient.Name?.Any() != true) return patient.Id ?? "";
        var n = patient.Name.First();
        var parts = new List<string>();
        if (n.Family != null) parts.Add(n.Family);
        if (n.Given != null)
            foreach (var g in n.Given)
                if (g?.ToString() is { } s && s.Length > 0) parts.Add(s);
        return string.Join(" ", parts);
    }
}
