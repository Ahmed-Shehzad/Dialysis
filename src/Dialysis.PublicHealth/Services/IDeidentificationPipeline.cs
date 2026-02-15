using Hl7.Fhir.Model;

namespace Dialysis.PublicHealth.Services;

/// <summary>De-identification pipeline for FHIR resources. Supports Basic, HIPAA Safe Harbor, and Expert Determination.</summary>
public interface IDeidentificationPipeline
{
    Task<Resource> DeidentifyAsync(Resource resource, DeidentificationOptions options, CancellationToken cancellationToken = default);
    Task<Bundle> DeidentifyBundleAsync(Bundle bundle, DeidentificationOptions options, CancellationToken cancellationToken = default);
}

/// <summary>De-identification level per HIPAA guidance.</summary>
public enum DeidentificationLevel
{
    /// <summary>Basic: names, free-text, some identifiers.</summary>
    Basic = 0,

    /// <summary>HIPAA Safe Harbor: all 18 identifiers removed/generalized.</summary>
    SafeHarbor = 1,

    /// <summary>Expert Determination: configurable rules; document risk assessment.</summary>
    ExpertDetermination = 2
}

public sealed record DeidentificationOptions
{
    public DeidentificationLevel Level { get; init; } = DeidentificationLevel.Basic;

    /// <summary>Remove direct identifiers (name, SSN, MRN, etc.).</summary>
    public bool RemoveDirectIdentifiers { get; init; } = true;

    /// <summary>Generalize dates to year only (birth, admission, discharge).</summary>
    public bool GeneralizeDates { get; init; } = true;

    /// <summary>Remove free-text narrative that may contain PHI.</summary>
    public bool RemoveFreeText { get; init; } = true;

    /// <summary>For SafeHarbor: generalize ages over 89 to "90 or older".</summary>
    public bool GeneralizeAgesOver89 { get; init; } = true;

    /// <summary>For ExpertDetermination: custom suppression list (element paths to redact).</summary>
    public IReadOnlyList<string>? CustomSuppressions { get; init; }

    /// <summary>For ExpertDetermination: risk assessment notes (documentation only).</summary>
    public string? RiskAssessmentNotes { get; init; }
}
