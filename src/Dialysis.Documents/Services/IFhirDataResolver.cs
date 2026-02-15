namespace Dialysis.Documents.Services;

/// <summary>Resolves FHIR resource data to field name/value pairs for PDF form filling.</summary>
public interface IFhirDataResolver
{
    Task<IReadOnlyDictionary<string, string>> GetPatientFieldValuesAsync(string patientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, string>> GetEncounterFieldValuesAsync(string encounterId, CancellationToken cancellationToken = default);
}
