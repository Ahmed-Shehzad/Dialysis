namespace Dialysis.HIE.Inbound.Mpi;

/// <summary>
/// Tunables for probabilistic MPI matching, bound from <c>Hie:Mpi</c>. Field weights are relative
/// (normalised at scoring time); thresholds map a 0–1 score to a <see cref="MatchGrade"/>. Defaults
/// favour MRN + DOB, with Jaro-Winkler name similarity carrying the rest. A cross-source match that
/// lands at <see cref="ProbableThreshold"/> (but below auto-link certainty) is queued for a steward.
/// </summary>
public sealed class MpiMatchOptions
{
    public const string SectionName = "Hie:Mpi";

    public double MrnWeight { get; set; } = 0.45;
    public double FamilyNameWeight { get; set; } = 0.20;
    public double GivenNameWeight { get; set; } = 0.15;
    public double DateOfBirthWeight { get; set; } = 0.15;
    public double SexWeight { get; set; } = 0.05;

    /// <summary>≥ this score is treated as the same person (safe to auto-link).</summary>
    public double CertainThreshold { get; set; } = 0.92;

    /// <summary>≥ this score is a likely duplicate — queued for steward review.</summary>
    public double ProbableThreshold { get; set; } = 0.78;

    /// <summary>≥ this score is surfaced as a possible match (e.g. on $match), below it is dropped.</summary>
    public double PossibleThreshold { get; set; } = 0.55;
}
