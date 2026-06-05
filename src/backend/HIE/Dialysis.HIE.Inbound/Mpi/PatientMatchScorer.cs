using Dialysis.HIE.Inbound.Domain;

namespace Dialysis.HIE.Inbound.Mpi;

/// <summary>How confident the MPI is that two records are the same person (FHIR `$match` grade).</summary>
public enum MatchGrade
{
    /// <summary>Below the possible-match floor — not surfaced as a match.</summary>
    NoMatch = 0,

    /// <summary>Weak similarity — surface for a steward to inspect, never auto-link.</summary>
    Possible = 1,

    /// <summary>Strong similarity — a likely duplicate; steward review recommended before linking.</summary>
    Probable = 2,

    /// <summary>Effectively certain (e.g. exact MRN + demographics) — safe to auto-link.</summary>
    Certain = 3,
}

/// <summary>Search criteria for a probabilistic patient match.</summary>
public sealed record PatientMatchCriteria(
    string? Mrn,
    string? FamilyName,
    string? GivenName,
    DateOnly? DateOfBirth,
    string? SexAtBirthCode);

/// <summary>The scored outcome of comparing criteria against one index entry.</summary>
public sealed record PatientMatchScore(double Score, MatchGrade Grade);

/// <summary>
/// Probabilistic record-linkage scorer for the MPI. Each populated field contributes its weight ×
/// field-similarity; the score is normalised by the total applicable weight, then classified into a
/// <see cref="MatchGrade"/> by the configured thresholds. An exact MRN match is weighted heavily, so
/// MRN-equal records score near-certain; names use Jaro-Winkler so typos/transpositions still link.
/// </summary>
public sealed class PatientMatchScorer
{
    private readonly MpiMatchOptions _options;

    public PatientMatchScorer(MpiMatchOptions options) => _options = options;

    public PatientMatchScore Score(PatientMatchCriteria criteria, PatientIndexEntry entry)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        ArgumentNullException.ThrowIfNull(entry);

        double weightedSum = 0;
        double totalWeight = 0;

        // MRN — exact identifier match (case-insensitive); decisive when both sides have one.
        if (!string.IsNullOrWhiteSpace(criteria.Mrn) && !string.IsNullOrWhiteSpace(entry.MedicalRecordNumber))
        {
            var sim = string.Equals(criteria.Mrn.Trim(), entry.MedicalRecordNumber.Trim(), StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
            weightedSum += _options.MrnWeight * sim;
            totalWeight += _options.MrnWeight;
        }

        if (!string.IsNullOrWhiteSpace(criteria.FamilyName) && !string.IsNullOrWhiteSpace(entry.FamilyName))
        {
            weightedSum += _options.FamilyNameWeight * StringSimilarity.JaroWinkler(criteria.FamilyName, entry.FamilyName);
            totalWeight += _options.FamilyNameWeight;
        }

        if (!string.IsNullOrWhiteSpace(criteria.GivenName) && !string.IsNullOrWhiteSpace(entry.GivenName))
        {
            weightedSum += _options.GivenNameWeight * StringSimilarity.JaroWinkler(criteria.GivenName, entry.GivenName);
            totalWeight += _options.GivenNameWeight;
        }

        if (criteria.DateOfBirth is { } dob && entry.DateOfBirth is { } entryDob)
        {
            weightedSum += _options.DateOfBirthWeight * (dob == entryDob ? 1.0 : 0.0);
            totalWeight += _options.DateOfBirthWeight;
        }

        if (!string.IsNullOrWhiteSpace(criteria.SexAtBirthCode) && !string.IsNullOrWhiteSpace(entry.SexAtBirthCode))
        {
            var sim = string.Equals(criteria.SexAtBirthCode.Trim(), entry.SexAtBirthCode.Trim(), StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
            weightedSum += _options.SexWeight * sim;
            totalWeight += _options.SexWeight;
        }

        var score = totalWeight <= 0 ? 0.0 : weightedSum / totalWeight;
        return new PatientMatchScore(score, Classify(score));
    }

    private MatchGrade Classify(double score) =>
        score >= _options.CertainThreshold ? MatchGrade.Certain
        : score >= _options.ProbableThreshold ? MatchGrade.Probable
        : score >= _options.PossibleThreshold ? MatchGrade.Possible
        : MatchGrade.NoMatch;
}
