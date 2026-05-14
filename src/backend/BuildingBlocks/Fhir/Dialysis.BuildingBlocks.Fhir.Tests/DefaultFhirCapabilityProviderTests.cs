using Hl7.Fhir.Model;
using Shouldly;
using Xunit;

namespace Dialysis.BuildingBlocks.Fhir.Tests;

public sealed class DefaultFhirCapabilityProviderTests
{
    [Fact]
    public void Reflects_Only_Registered_Resources()
    {
        var registry = new FhirResourceRegistry();
        registry.RegisterReader<Patient>();
        registry.RegisterSearcher<Observation>();

        var provider = new DefaultFhirCapabilityProvider(registry);

        var resources = provider.DescribeResources();
        resources.Count.ShouldBe(2);
        resources.ShouldContain(r => r.Type == "Patient" && r.Interaction.Any(i => i.Code == CapabilityStatement.TypeRestfulInteraction.Read));
        resources.ShouldContain(r => r.Type == "Observation" && r.Interaction.Any(i => i.Code == CapabilityStatement.TypeRestfulInteraction.SearchType));
    }

    [Fact]
    public void Fhirversion_Is_R4()
    {
        IFhirCapabilityProvider provider = new DefaultFhirCapabilityProvider(new FhirResourceRegistry());

        provider.FhirVersion.ShouldBe("4.0.1");
    }
}
