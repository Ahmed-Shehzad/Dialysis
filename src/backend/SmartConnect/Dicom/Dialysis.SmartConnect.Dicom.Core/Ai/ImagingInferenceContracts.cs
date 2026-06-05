namespace Dialysis.SmartConnect.Dicom.Ai;

/// <summary>Coarse interpretation of an AI imaging finding.</summary>
public enum ImagingFindingInterpretation
{
    Normal = 0,
    Abnormal = 1,
    Indeterminate = 2,
}

/// <summary>
/// De-identified input to an imaging inference provider. Deliberately carries no PHI — only the
/// study UID, modality, an optional body-site hint, and the accession number — so the request can
/// cross a provider boundary (edge model or vendor API) safely. A real pixel-reading provider must
/// de-identify the pixel data (via <c>BuildingBlocks/Fhir.DeIdentification</c>) before this hop.
/// </summary>
public sealed record ImagingInferenceRequest(
    string StudyInstanceUid,
    string? Modality,
    string? BodySiteHint,
    string? AccessionNumber);

/// <summary>
/// A single coded finding an inference provider produced. <see cref="Confidence"/> is 0–1.
/// Findings are advisory: the <see cref="ImagingAiAnalyzer"/> wraps them as requiring human review.
/// </summary>
public sealed record ImagingFinding(
    string Code,
    string System,
    string Display,
    double Confidence,
    ImagingFindingInterpretation Interpretation,
    string Summary);

/// <summary>
/// The analyzer's governed output: the model that ran, the finding, and the
/// human-in-the-loop flag (always set — AI output is never auto-final/diagnostic).
/// </summary>
public sealed record ImagingAiAssessment(
    string ModelId,
    ImagingFinding Finding,
    bool RequiresHumanReview,
    DateTimeOffset ProducedAtUtc);
