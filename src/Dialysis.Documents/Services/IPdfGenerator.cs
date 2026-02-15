using Hl7.Fhir.Model;

namespace Dialysis.Documents.Services;

/// <summary>Generates PDF from FHIR resources (Patient, Encounter, MeasureReport, etc.).</summary>
public interface IPdfGenerator
{
    /// <summary>Generate PDF from FHIR data. Template: session-summary, patient-summary, measure-report.</summary>
    /// <param name="template">Template name.</param>
    /// <param name="patientId">Patient ID (required for session-summary, patient-summary).</param>
    /// <param name="encounterId">Encounter ID (optional for session-summary).</param>
    /// <param name="resourceId">Resource ID when generating from single resource (e.g. MeasureReport).</param>
    /// <param name="bundle">Inline FHIR Bundle (alternative to fetching by IDs).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PDF bytes.</returns>
    Task<byte[]> GenerateAsync(
        string template,
        string? patientId,
        string? encounterId,
        string? resourceId,
        Bundle? bundle,
        CancellationToken cancellationToken = default);
}
