using Dialysis.BuildingBlocks.Fhir.Mapping;
using Dialysis.BuildingBlocks.Fhir.Testing;
using Hl7.Fhir.Model;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Dialysis.BuildingBlocks.Fhir.Tests;

public sealed class FhirBuilderTests
{
    [Fact]
    public void Addreader_Registers_Service_And_Dispatcher()
    {
        var services = new ServiceCollection();

        services.AddFhir(fhir => fhir.AddReader<Patient, FakeFhirReader<Patient>>());

        using var provider = services.BuildServiceProvider();
        var reader = provider.GetRequiredService<IFhirReader<Patient>>();
        reader.ShouldBeOfType<FakeFhirReader<Patient>>();

        var registry = provider.GetRequiredService<FhirResourceRegistry>();
        registry.TryGetReadDispatcher("Patient", out _).ShouldBeTrue();
    }

    [Fact]
    public void Addfhir_Is_Idempotent_And_Registry_Is_Shared()
    {
        var services = new ServiceCollection();
        services.AddFhir(fhir => fhir.AddReader<Patient, FakeFhirReader<Patient>>());
        services.AddFhir(fhir => fhir.AddReader<Observation, FakeFhirReader<Observation>>());

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<FhirResourceRegistry>();

        registry.Entries.ShouldContainKey("Patient");
        registry.Entries.ShouldContainKey("Observation");
    }

    [Fact]
    public void Useconsentgate_Replaces_Default_Noop()
    {
        var services = new ServiceCollection();

        services.AddFhir(fhir => fhir.UseConsentGate<FakeConsentGate>());

        using var provider = services.BuildServiceProvider();
        var gate = provider.GetRequiredService<IFhirConsentGate>();
        gate.ShouldBeOfType<FakeConsentGate>();
    }

    [Fact]
    public void Addmapper_Registers_All_Implemented_Mapper_Interfaces()
    {
        var services = new ServiceCollection();

        services.AddFhir(fhir => fhir.AddMapper<DemoMapper>());

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IFhirResourceMapper<DemoSource, Patient>>()
            .ShouldBeOfType<DemoMapper>();
    }

    private sealed record DemoSource
    {
        public DemoSource(string Id) => this.Id = Id;
        public string Id { get; init; }
        public void Deconstruct(out string id) => id = this.Id;
    }

    private sealed class DemoMapper : IFhirResourceMapper<DemoSource, Patient>
    {
        public Patient Map(DemoSource source) => new() { Id = source.Id };
    }
}
