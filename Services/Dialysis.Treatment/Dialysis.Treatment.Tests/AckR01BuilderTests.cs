using Dialysis.Treatment.Infrastructure.Hl7;

using Shouldly;

namespace Dialysis.Treatment.Tests;

public sealed class AckR01BuilderTests
{
    private readonly AckR01Builder _builder = new();

    [Fact]
    public void BuildAccept_ContainsMshWithAckR01()
    {
        string ack = _builder.BuildAccept("MSG001");

        ack.ShouldContain("MSH|");
        ack.ShouldContain("ACK^R01^ACK");
        ack.ShouldContain("MSG001");
    }

    [Fact]
    public void BuildAccept_MsaShowsAA()
    {
        string ack = _builder.BuildAccept("MSG001");

        ack.ShouldContain("MSA|AA|MSG001");
    }

    [Fact]
    public void BuildAccept_DoesNotContainErr()
    {
        string ack = _builder.BuildAccept("MSG001");

        ack.ShouldNotContain("ERR|");
    }

    [Fact]
    public void BuildError_MsaShowsAE()
    {
        string ack = _builder.BuildError("MSG002", "Invalid message format");

        ack.ShouldContain("MSA|AE|MSG002");
        ack.ShouldContain("ERR|");
        ack.ShouldContain("Invalid message format");
    }
}
