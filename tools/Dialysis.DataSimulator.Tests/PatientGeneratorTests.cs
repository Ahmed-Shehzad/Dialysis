using Shouldly;
using Xunit;

namespace Dialysis.DataSimulator.Tests;

public sealed class PatientGeneratorTests
{
    [Fact]
    public void Same_Seed_And_Sequence_Produce_The_Same_Patient()
    {
        var first = PatientGenerator.Generate(7, 42);
        var second = PatientGenerator.Generate(7, 42);

        second.ShouldBe(first);
        first.MedicalRecordNumber.ShouldStartWith("MRN-");
        first.SexAtBirthCode.ShouldBeOneOf("M", "F");
        first.ProviderId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Different_Sequence_Produces_A_Different_Patient()
    {
        var first = PatientGenerator.Generate(7, 42);
        var second = PatientGenerator.Generate(7, 43);

        second.ShouldNotBe(first);
    }
}
