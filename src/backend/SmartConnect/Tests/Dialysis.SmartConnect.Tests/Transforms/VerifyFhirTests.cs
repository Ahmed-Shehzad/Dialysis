using System.Collections.Immutable;
using System.Text;
using Dialysis.BuildingBlocks.Fhir.Validation;
using Dialysis.SmartConnect.Transforms;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.SmartConnect.Tests.Transforms;

/// <summary>
/// Covers the verify-fhir plugins. Uses the real <c>DefaultFhirProfileValidator</c> (registered by
/// <c>SmartConnectServiceCollectionExtensions.AddSmartConnectCore</c>) with an empty profile map —
/// the validator then only enforces the FHIR resource shape itself, which is enough to exercise
/// the parse / pass / fail branches.
/// </summary>
public sealed class VerifyFhirTests
{
    private static IFhirProfileValidator BuildValidator()
    {
        var services = new ServiceCollection();
        services.AddFhirProfileValidation(_ => { });
        return services.BuildServiceProvider().GetRequiredService<IFhirProfileValidator>();
    }

    [Fact]
    public async Task Filter_Allows_Well_Formed_Patient_Async()
    {
        var validator = BuildValidator();
        var filter = new VerifyFhirRouteFilter(validator);
        var json = Serialize(new Patient { Id = "p1", Active = true });
        var result = await filter.EvaluateAsync(NewMessage(json), CancellationToken.None);
        Assert.Equal(RouteFilterDisposition.Allow, result.Disposition);
    }

    [Fact]
    public async Task Filter_Drops_Non_Fhir_Payload_Async()
    {
        var validator = BuildValidator();
        var filter = new VerifyFhirRouteFilter(validator);
        var result = await filter.EvaluateAsync(NewMessage("not-json-not-fhir"), CancellationToken.None);
        Assert.Equal(RouteFilterDisposition.Drop, result.Disposition);
    }

    [Fact]
    public async Task Strict_Stage_Throws_On_Non_Fhir_Async()
    {
        var validator = BuildValidator();
        var stage = new VerifyFhirTransformStage(validator);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await stage.TransformAsync(NewMessage("{\"not\":\"a fhir resource\"}"), CancellationToken.None));
    }

    [Fact]
    public async Task Strict_Stage_Passes_Valid_Resource_Through_Unchanged_Async()
    {
        var validator = BuildValidator();
        var stage = new VerifyFhirTransformStage(validator);
        var json = Serialize(new Observation
        {
            Id = "o1",
            Status = ObservationStatus.Final,
            Code = new CodeableConcept("http://loinc.org", "29463-7", "Body weight"),
        });
        var input = NewMessage(json);
        var output = await stage.TransformAsync(input, CancellationToken.None);
        Assert.Equal(input.Payload.ToArray(), output.Payload.ToArray());
    }

    private static string Serialize(Resource resource) => new FhirJsonSerializer().SerializeToString(resource);

    private static IntegrationMessage NewMessage(string payload) => new()
    {
        Id = Guid.CreateVersion7(),
        FlowId = Guid.CreateVersion7(),
        CorrelationId = "c-" + Guid.NewGuid().ToString("N")[..8],
        Payload = Encoding.UTF8.GetBytes(payload),
        PayloadFormat = PayloadFormat.Json,
        Metadata = ImmutableDictionary<string, string>.Empty,
        ReceivedAtUtc = DateTimeOffset.UtcNow,
    };
}
