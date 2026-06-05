using Dialysis.HIE.Inbound.Domain;
using Dialysis.HIE.Inbound.Ports;

namespace Dialysis.HIE.Inbound.Mpi;

/// <summary>One scored candidate from a probabilistic match, most-confident first.</summary>
public sealed record ScoredPatientMatch(PatientIndexEntry Entry, double Score, MatchGrade Grade);

/// <summary>
/// Probabilistic MPI matching: pulls a blocking candidate set from <see cref="IPatientIndex"/>,
/// scores each with <see cref="PatientMatchScorer"/>, drops everything below the possible-match
/// floor, and returns the rest ranked by score. Backs <c>Patient/$match</c> and the steward
/// duplicate-detection pass.
/// </summary>
public sealed class PatientMatchService
{
    private readonly IPatientIndex _index;
    private readonly PatientMatchScorer _scorer;

    public PatientMatchService(IPatientIndex index, PatientMatchScorer scorer)
    {
        _index = index;
        _scorer = scorer;
    }

    public async Task<IReadOnlyList<ScoredPatientMatch>> FindMatchesAsync(
        PatientMatchCriteria criteria, int take, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var candidates = await _index
            .MatchCandidatesAsync(criteria.Mrn, criteria.FamilyName, criteria.DateOfBirth, take: Math.Max(take, 50), cancellationToken)
            .ConfigureAwait(false);

        return [.. candidates
            .Select(c => new { Entry = c, Score = _scorer.Score(criteria, c) })
            .Where(x => x.Score.Grade != MatchGrade.NoMatch)
            .OrderByDescending(x => x.Score.Score)
            .Take(take)
            .Select(x => new ScoredPatientMatch(x.Entry, x.Score.Score, x.Score.Grade))];
    }
}
