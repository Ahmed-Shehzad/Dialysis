using Dialysis.Simulation.Engine.Generation;
using Shouldly;
using Xunit;

namespace Dialysis.Simulation.Tests;

public sealed class DeterministicSeedTests
{
    private readonly BogusJourneyGenerator _generator = new();

    [Fact]
    public void Same_Inputs_Produce_The_Same_Journey()
    {
        var first = _generator.Generate("outpatient-lab", "tenant-a", 42);
        var second = _generator.Generate("outpatient-lab", "tenant-a", 42);

        second.ShouldBe(first);
        first.MedicalRecordNumber.ShouldStartWith("SIM-");
        first.SexAtBirthCode.ShouldBeOneOf("M", "F");
    }

    [Fact]
    public void Different_Seed_Produces_A_Different_Journey()
    {
        var first = _generator.Generate("outpatient-lab", "tenant-a", 42);
        var second = _generator.Generate("outpatient-lab", "tenant-a", 43);

        second.ShouldNotBe(first);
    }

    [Fact]
    public void Different_Tenant_Produces_A_Different_Journey()
    {
        var first = _generator.Generate("outpatient-lab", "tenant-a", 42);
        var second = _generator.Generate("outpatient-lab", "tenant-b", 42);

        second.ShouldNotBe(first);
    }
}
