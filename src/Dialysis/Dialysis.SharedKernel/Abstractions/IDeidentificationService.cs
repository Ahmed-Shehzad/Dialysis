namespace Dialysis.SharedKernel.Abstractions;

/// <summary>
/// De-identification for regulatory reporting. Phase 2.2.3.
/// Strip or generalize identifiers for NHSN, registries, analytics.
/// </summary>
public interface IDeidentificationService
{
    /// <summary>
    /// Apply de-identification to FHIR bundle JSON.
    /// Returns null if service is not configured.
    /// </summary>
    Task<string?> DeidentifyAsync(string fhirBundleJson, CancellationToken cancellationToken = default);
}
