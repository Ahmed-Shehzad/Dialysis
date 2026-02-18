using Dialysis.Alarm.Infrastructure.Hl7;

using Shouldly;

namespace Dialysis.Alarm.Tests;

public sealed class OraR41BuilderTests
{
    private readonly OraR41Builder _builder = new();

    [Fact]
    public void BuildAccept_ContainsMshWithOraR41()
    {
        string ora = _builder.BuildAccept("MSG001");

        ora.ShouldContain("MSH|");
        ora.ShouldContain("ORA^R41^ORA_R41");
        ora.ShouldContain("MSG001");
    }

    [Fact]
    public void BuildAccept_MsaShowsAA()
    {
        string ora = _builder.BuildAccept("MSG001");

        ora.ShouldContain("MSA|AA|MSG001");
    }

    [Fact]
    public void BuildAccept_DoesNotContainErr()
    {
        string ora = _builder.BuildAccept("MSG001");

        ora.ShouldNotContain("ERR|");
    }

    [Fact]
    public void BuildError_MsaShowsAE()
    {
        string ora = _builder.BuildError("MSG002", "Unknown alarm format");

        ora.ShouldContain("MSA|AE|MSG002");
        ora.ShouldContain("ERR|");
        ora.ShouldContain("Unknown alarm format");
    }
}
