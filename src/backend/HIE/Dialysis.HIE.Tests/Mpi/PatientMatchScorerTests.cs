using Dialysis.HIE.Inbound.Domain;
using Dialysis.HIE.Inbound.Mpi;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests.Mpi;

/// <summary>Coverage for probabilistic MPI scoring (Jaro-Winkler names + weighted fields + grades).</summary>
public sealed class PatientMatchScorerTests
{
    private readonly PatientMatchScorer _scorer = new(new MpiMatchOptions());

    private static PatientIndexEntry Entry(
        string? mrn, string? family, string? given, DateOnly? dob, string? sex = "male") =>
        new("partner-a", "ext-1", mrn, family, given, dob, sex, DateTime.UtcNow);

    private static readonly DateOnly _dob = new(1980, 5, 1);

    [Fact]
    public void Identical_Demographics_Score_Certain()
    {
        var criteria = new PatientMatchCriteria("MRN-1", "Smith", "John", _dob, "male");
        var result = _scorer.Score(criteria, Entry("MRN-1", "Smith", "John", _dob));

        result.Grade.ShouldBe(MatchGrade.Certain);
        result.Score.ShouldBeGreaterThan(0.92);
    }

    [Fact]
    public void Name_Typo_With_Matching_Mrn_And_Dob_Still_Certain()
    {
        // MRN + DOB exact, family transposed ("Smtih"), given typo ("Jon") — record linkage holds.
        var criteria = new PatientMatchCriteria("MRN-1", "Smtih", "Jon", _dob, "male");
        var result = _scorer.Score(criteria, Entry("MRN-1", "Smith", "John", _dob));

        result.Grade.ShouldBeOneOf(MatchGrade.Certain, MatchGrade.Probable);
        result.Score.ShouldBeGreaterThan(0.78);
    }

    [Fact]
    public void Same_Name_And_Dob_Without_Mrn_Is_Probable()
    {
        // No MRN on either side; strong name + DOB → probable (steward review territory).
        var criteria = new PatientMatchCriteria(null, "Smith", "John", _dob, "male");
        var result = _scorer.Score(criteria, Entry(null, "Smith", "John", _dob));

        result.Grade.ShouldBeOneOf(MatchGrade.Probable, MatchGrade.Certain);
    }

    [Fact]
    public void Different_Person_Is_No_Match()
    {
        var criteria = new PatientMatchCriteria("MRN-9", "Johnson", "Alice", new DateOnly(1991, 2, 2), "female");
        var result = _scorer.Score(criteria, Entry("MRN-1", "Smith", "John", _dob));

        result.Grade.ShouldBe(MatchGrade.NoMatch);
        result.Score.ShouldBeLessThan(0.55);
    }

    [Fact]
    public void Conflicting_Mrn_But_Same_Name_Dob_Drops_Below_Certain()
    {
        // Same person demographically but a different MRN → not auto-link; steward should decide.
        var criteria = new PatientMatchCriteria("MRN-2", "Smith", "John", _dob, "male");
        var result = _scorer.Score(criteria, Entry("MRN-1", "Smith", "John", _dob));

        result.Grade.ShouldBeOneOf(MatchGrade.Probable, MatchGrade.Possible);
        result.Score.ShouldBeLessThan(0.92);
    }

    [Fact]
    public void Jaro_Winkler_Rewards_Shared_Prefix()
    {
        StringSimilarity.JaroWinkler("MARTHA", "MARHTA").ShouldBeGreaterThan(0.95);
        StringSimilarity.JaroWinkler("Smith", "smith").ShouldBe(1.0); // case-insensitive
        StringSimilarity.JaroWinkler("abc", "xyz").ShouldBeLessThan(0.5);
    }
}
