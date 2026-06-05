using System.Globalization;

namespace Dialysis.SmartConnect.Dicom.Ai;

/// <summary>
/// A deterministic, NON-DIAGNOSTIC sample inference provider shipped so the AI pipeline has a
/// reference implementation behind <see cref="IImagingInferenceProvider"/> — never a real model.
/// It maps modality + body-site to a canned RadLex-coded finding so the end-to-end flow (gate →
/// analyze → audit → human review) can be exercised without a vendor dependency. Confidence is
/// intentionally modest and the text says "sample"; do not use for clinical decisions.
/// </summary>
public sealed class SampleHeuristicImagingInferenceProvider : IImagingInferenceProvider
{
    /// <inheritdoc />
    public string ModelId => "sample-heuristic-v0";

    /// <inheritdoc />
    public Task<ImagingFinding?> AnalyzeAsync(ImagingInferenceRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var modality = (request.Modality ?? string.Empty).ToUpperInvariant();
        var site = (request.BodySiteHint ?? string.Empty).ToUpperInvariant();

        var finding = (modality, site) switch
        {
            ("US", var s) when s.Contains("VASCULAR", StringComparison.Ordinal) || s.Contains("FISTULA", StringComparison.Ordinal) =>
                Finding("RID39055", "Patent vascular access, no flow-limiting stenosis", 0.62, ImagingFindingInterpretation.Normal),
            ("US", var s) when s.Contains("KIDNEY", StringComparison.Ordinal) || s.Contains("RENAL", StringComparison.Ordinal) =>
                Finding("RID35811", "Kidneys normal in size and echogenicity", 0.58, ImagingFindingInterpretation.Normal),
            ("US", _) =>
                Finding("RID35811", "Ultrasound within normal limits", 0.54, ImagingFindingInterpretation.Indeterminate),
            ("CR", _) or ("DX", _) =>
                Finding("RID35811", "No acute cardiopulmonary finding", 0.55, ImagingFindingInterpretation.Normal),
            ("CT", _) =>
                Finding("RID35825", "No acute abnormality detected", 0.53, ImagingFindingInterpretation.Indeterminate),
            _ => null,
        };

        return Task.FromResult(finding);
    }

    private static ImagingFinding Finding(string code, string display, double confidence, ImagingFindingInterpretation interpretation) =>
        new(
            Code: code,
            System: "http://radlex.org",
            Display: display,
            Confidence: confidence,
            Interpretation: interpretation,
            Summary: $"[SAMPLE MODEL — not for clinical use] {display} (confidence {confidence.ToString("0.00", CultureInfo.InvariantCulture)}).");
}
