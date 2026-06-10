using Microsoft.Extensions.Options;

namespace Dialysis.EHR.Billing.Coding;

/// <summary>A suggested E/M visit level for documented diagnoses + data.</summary>
public sealed record EmSuggestion(string SuggestedCptCode, int Level, string Rationale);

/// <summary>
/// Deterministic E/M coding assist: maps the documented number of problems + data reviewed to the
/// highest qualifying E/M level (config-driven, empty → no suggestion). Also exposes the level of a
/// captured CPT so the charge-edit path can spot under-coding. No external coding service.
/// </summary>
public interface IEvaluationManagementCoder
{
    /// <summary>The highest E/M level whose thresholds the documentation meets, or null when none/unconfigured.</summary>
    EmSuggestion? Suggest(IReadOnlyCollection<string> diagnosisIcd10, IReadOnlyCollection<string> procedureCpt, int dataReviewedCount);

    /// <summary>The configured level of a CPT code if it is a known E/M code, else null.</summary>
    int? LevelOf(string cptCode);

    /// <summary>True when the CPT is one of the configured E/M codes.</summary>
    bool IsEmCode(string cptCode);
}

public sealed class EvaluationManagementCoder : IEvaluationManagementCoder
{
    private readonly EmCodingOptions _options;
    public EvaluationManagementCoder(IOptions<EmCodingOptions> options) => _options = options.Value;

    public EmSuggestion? Suggest(IReadOnlyCollection<string> diagnosisIcd10, IReadOnlyCollection<string> procedureCpt, int dataReviewedCount)
    {
        if (_options.Levels.Count == 0)
            return null;

        var diagnoses = diagnosisIcd10?.Count ?? 0;
        var best = _options.Levels
            .Where(r => diagnoses >= r.MinDiagnoses && dataReviewedCount >= r.MinDataReviewed)
            .OrderByDescending(r => r.Level)
            .FirstOrDefault();

        return best is null ? null : new EmSuggestion(best.CptCode, best.Level, best.Rationale);
    }

    public int? LevelOf(string cptCode)
    {
        var cpt = cptCode?.Trim() ?? string.Empty;
        var rule = _options.Levels.Find(r => r.CptCode.Equals(cpt, StringComparison.OrdinalIgnoreCase));
        return rule?.Level;
    }

    public bool IsEmCode(string cptCode)
    {
        var cpt = cptCode?.Trim() ?? string.Empty;
        if (cpt.Length == 0)
            return false;
        if (_options.EmCptCodes.Count > 0)
            return _options.EmCptCodes.Exists(c => c.Trim().Equals(cpt, StringComparison.OrdinalIgnoreCase));
        return _options.Levels.Exists(r => r.CptCode.Equals(cpt, StringComparison.OrdinalIgnoreCase));
    }
}
