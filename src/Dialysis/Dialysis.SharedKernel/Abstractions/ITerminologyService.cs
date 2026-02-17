namespace Dialysis.SharedKernel.Abstractions;

/// <summary>
/// Terminology lookup (LOINC, SNOMED, etc.). Phase 4.3.2.
/// </summary>
public interface ITerminologyService
{
    /// <summary>
    /// Look up display text for a code in the given system.
    /// Returns null if not found.
    /// </summary>
    Task<string?> LookupDisplayAsync(string system, string code, CancellationToken cancellationToken = default);
}
