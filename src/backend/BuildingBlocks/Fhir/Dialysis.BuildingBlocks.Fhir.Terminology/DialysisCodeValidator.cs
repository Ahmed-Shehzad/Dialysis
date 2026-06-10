using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Terminology;

/// <summary>Outcome of a <c>$validate-code</c> against a governed value set.</summary>
public sealed record CodeValidationResult(bool IsValid, string? Display);

/// <summary>One target of a <c>$translate</c> through a governed concept map.</summary>
public sealed record CodeTranslationResult(string TargetSystem, string TargetCode, string? TargetDisplay);

/// <summary>
/// A thin, FHIR-free facade over the governed <see cref="DialysisTerminologyCatalog"/> for callers that
/// only need to gate or normalise a code at the point it is produced (LIS result coding, imaging-AI
/// findings) and don't want to take a dependency on the raw <see cref="Parameters"/> shape. Backed by the
/// in-memory catalog so it answers deterministically without an upstream tx server.
/// </summary>
public interface IDialysisCodeValidator
{
    /// <summary>Validates <paramref name="code"/> (optionally constrained to <paramref name="system"/>) against a value set.</summary>
    ValueTask<CodeValidationResult> ValidateAsync(string valueSetUrl, string code, string? system, CancellationToken cancellationToken);

    /// <summary>Translates a source code through a concept map; null when the map has no matching target.</summary>
    ValueTask<CodeTranslationResult?> TranslateAsync(string conceptMapUrl, string sourceSystem, string sourceCode, CancellationToken cancellationToken);
}

/// <summary>Default <see cref="IDialysisCodeValidator"/> over the seeded <see cref="DialysisTerminologyCatalog"/>.</summary>
public sealed class DialysisCodeValidator : IDialysisCodeValidator
{
    private readonly DialysisTerminologyCatalog _catalog;

    /// <summary>Creates the validator over the governed catalog.</summary>
    public DialysisCodeValidator(DialysisTerminologyCatalog catalog) => _catalog = catalog;

    /// <inheritdoc />
    public async ValueTask<CodeValidationResult> ValidateAsync(
        string valueSetUrl, string code, string? system, CancellationToken cancellationToken)
    {
        var result = await _catalog.Service.ValidateCodeAsync(valueSetUrl, code, system, cancellationToken).ConfigureAwait(false);
        var isValid = result.Parameter.Find(p => p.Name == "result")?.Value is FhirBoolean { Value: true };
        if (!isValid)
        {
            return new CodeValidationResult(false, null);
        }

        // Display is best-effort: only populated when the code's system is itself a catalog CodeSystem.
        string? display = null;
        if (!string.IsNullOrWhiteSpace(system))
        {
            var lookup = await _catalog.Service.LookupAsync(system, code, cancellationToken).ConfigureAwait(false);
            display = (lookup.Parameter.Find(p => p.Name == "display")?.Value as FhirString)?.Value;
        }

        return new CodeValidationResult(true, display);
    }

    /// <inheritdoc />
    public async ValueTask<CodeTranslationResult?> TranslateAsync(
        string conceptMapUrl, string sourceSystem, string sourceCode, CancellationToken cancellationToken)
    {
        var result = await _catalog.Service.TranslateAsync(conceptMapUrl, sourceSystem, sourceCode, cancellationToken).ConfigureAwait(false);
        var match = result.Parameter.Find(p => p.Name == "match");
        if (match?.Part.Find(p => p.Name == "concept")?.Value is not Coding concept || concept.Code is null || concept.System is null)
        {
            return null;
        }

        return new CodeTranslationResult(concept.System, concept.Code, concept.Display);
    }
}
