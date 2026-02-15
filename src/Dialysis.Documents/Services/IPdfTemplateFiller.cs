namespace Dialysis.Documents.Services;

/// <summary>Fills AcroForm PDF template fields with values from FHIR (or explicit mappings).</summary>
public interface IPdfTemplateFiller
{
    /// <summary>Fill template PDF with field values. Keys are AcroForm field names; values come from FHIR or explicit mappings.</summary>
    /// <param name="templateId">Template identifier (filename without extension or registered name).</param>
    /// <param name="patientId">Patient ID to load FHIR Patient.</param>
    /// <param name="encounterId">Optional Encounter ID.</param>
    /// <param name="mappings">Explicit field name to value overrides. FHIR path keys supported when backend resolves.</param>
    /// <param name="includeScripts">Include JavaScript/calculators in output (Phase 14).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Filled PDF bytes.</returns>
    Task<byte[]> FillAsync(
        string templateId,
        string? patientId,
        string? encounterId,
        IReadOnlyDictionary<string, string>? mappings,
        bool includeScripts,
        CancellationToken cancellationToken = default);
}
