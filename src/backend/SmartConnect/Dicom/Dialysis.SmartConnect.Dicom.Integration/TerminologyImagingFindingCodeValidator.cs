using Dialysis.BuildingBlocks.Fhir.Terminology;
using Dialysis.SmartConnect.Dicom.Ai;

namespace Dialysis.SmartConnect.Dicom.Integration;

/// <summary>
/// Terminology-backed <see cref="IImagingFindingCodeValidator"/>: gates AI findings against the
/// platform's governed imaging value set (<see cref="DialysisTerminologyCatalog.DialysisImagingFindingsValueSet"/>)
/// via <c>$validate-code</c>. Wired by the DICOM imaging bridge so an enabled model can only surface a
/// code the platform has governed — ungoverned codes are dropped + audited upstream in the analyzer.
/// </summary>
internal sealed class TerminologyImagingFindingCodeValidator : IImagingFindingCodeValidator
{
    private readonly IDialysisCodeValidator _validator;

    public TerminologyImagingFindingCodeValidator(IDialysisCodeValidator validator) => _validator = validator;

    /// <inheritdoc />
    public async ValueTask<bool> IsGovernedAsync(string system, string code, CancellationToken cancellationToken)
    {
        var result = await _validator
            .ValidateAsync(DialysisTerminologyCatalog.DialysisImagingFindingsValueSet, code, system, cancellationToken)
            .ConfigureAwait(false);
        return result.IsValid;
    }
}
