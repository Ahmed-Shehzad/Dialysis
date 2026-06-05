namespace Dialysis.SmartConnect.Dicom.Ai;

/// <summary>
/// Governance seam: decides whether an AI finding's code belongs to the platform's governed imaging
/// value set. Kept FHIR-free here so the lean DICOM core takes no terminology dependency — a host wires
/// the terminology-backed implementation (see the DICOM imaging bridge). The default
/// <see cref="PermissiveImagingFindingCodeValidator"/> governs nothing (every code passes), so behaviour
/// is unchanged until a host opts in.
/// </summary>
public interface IImagingFindingCodeValidator
{
    /// <summary>True when (<paramref name="system"/>, <paramref name="code"/>) is in the governed imaging value set.</summary>
    ValueTask<bool> IsGovernedAsync(string system, string code, CancellationToken cancellationToken);
}

/// <summary>Default validator that governs nothing — every finding code is accepted (no terminology dependency).</summary>
public sealed class PermissiveImagingFindingCodeValidator : IImagingFindingCodeValidator
{
    /// <inheritdoc />
    public ValueTask<bool> IsGovernedAsync(string system, string code, CancellationToken cancellationToken) => new(true);
}
