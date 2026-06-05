namespace Dialysis.SmartConnect.Dicom.Ai;

/// <summary>
/// Governs AI-assisted imaging reads. Bound from <c>Dicom:Ai</c>. <see cref="Enabled"/> defaults to
/// <see langword="false"/> — AI inference is strictly opt-in, behind a feature flag, per the model-
/// governance posture. Findings below <see cref="MinConfidence"/> are dropped (never surfaced).
/// </summary>
public sealed class ImagingAiOptions
{
    public const string SectionName = "Dicom:Ai";

    /// <summary>Master feature flag — AI reads only run when explicitly enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Findings below this confidence (0–1) are discarded.</summary>
    public double MinConfidence { get; set; } = 0.5;
}
