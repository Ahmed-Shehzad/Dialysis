using Shouldly;
using Xunit;

namespace Dialysis.DataSimulator.Tests;

public sealed class PatientGeneratorTests
{
    private readonly PatientGenerator _generator = new();

    [Fact]
    public void Same_Seed_And_Sequence_Produce_The_Same_Patient()
    {
        var first = _generator.Generate(7, 42);
        var second = _generator.Generate(7, 42);

        second.ShouldBe(first);
        first.MedicalRecordNumber.ShouldStartWith("SIM-");
        first.SexAtBirthCode.ShouldBeOneOf("M", "F");
        first.ProviderId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Different_Sequence_Produces_A_Different_Patient()
    {
        var first = _generator.Generate(7, 42);
        var second = _generator.Generate(7, 43);

        second.ShouldNotBe(first);
    }
}
