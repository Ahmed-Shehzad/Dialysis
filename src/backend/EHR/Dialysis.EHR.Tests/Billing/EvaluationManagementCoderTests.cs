using Dialysis.EHR.Billing.Coding;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Billing;

public sealed class EvaluationManagementCoderTests
{
    private static EvaluationManagementCoder Coder(params EmLevelRule[] levels)
    {
        var options = new EmCodingOptions();
        foreach (var l in levels)
            options.Levels.Add(l);
        return new EvaluationManagementCoder(Options.Create(options));
    }

    private static EmLevelRule Level(string cpt, int level, int minDx) =>
        new() { CptCode = cpt, Level = level, MinDiagnoses = minDx, MinDataReviewed = 0, Rationale = $"{minDx}+ problems" };

    private static readonly EmLevelRule[] _ladder =
    [
        Level("99213", 3, 1),
        Level("99214", 4, 2),
        Level("99215", 5, 4),
    ];

    [Fact]
    public void Empty_Config_Suggests_Nothing()
    {
        Coder().Suggest(["E11.9"], [], 1).ShouldBeNull();
    }

    [Fact]
    public void Suggests_The_Highest_Qualifying_Level()
    {
        var coder = Coder(_ladder);
        coder.Suggest(["E11.9"], [], 1)!.SuggestedCptCode.ShouldBe("99213");
        coder.Suggest(["E11.9", "I10"], [], 2)!.SuggestedCptCode.ShouldBe("99214");
        coder.Suggest(["E11.9", "I10", "N18.6", "E78.5"], [], 4)!.SuggestedCptCode.ShouldBe("99215");
    }

    [Fact]
    public void Knows_Em_Codes_And_Their_Levels()
    {
        var coder = Coder(_ladder);
        coder.IsEmCode("99214").ShouldBeTrue();
        coder.IsEmCode("36415").ShouldBeFalse();
        coder.LevelOf("99214").ShouldBe(4);
        coder.LevelOf("36415").ShouldBeNull();
    }
}
