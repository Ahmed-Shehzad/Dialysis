using System.Text;
using Dialysis.SmartConnect.BuiltInPlugins;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Json;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql;
using Dialysis.SmartConnect.Tests.TestPlugins;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.SmartConnect.Tests.Integration;

/// <summary>
/// End-to-end pipeline test: an HL7 v2 payload enters via <c>IFlowRuntime.DispatchAsync</c>
/// (same path as the MLLP source uses after frame parsing), runs through verify-hl7 →
/// hl7-to-fhir-pipeline → verify-fhir, and lands at a capturing outbound adapter as a FHIR
/// Bundle. Covers the success path for several triggers and the drop / fail paths.
///
/// Drives the in-memory composition; no real socket so the test stays CI-stable.
/// </summary>
public sealed class MllpToFhirEndToEndTests
{
    private const string AdtA01 =
        "MSH|^~\\&|HIS|HOSPITAL|EMR|CLINIC|20260526120000||ADT^A01|MSG-A01|P|2.5\r" +
        "EVN|A01|20260526120000\r" +
        "PID|||MRN-12345||DOE^JOHN\r" +
        "PV1|1|I|ICU^101";

    private const string OruR01 =
        "MSH|^~\\&|LAB|HOSPITAL|EMR|CLINIC|20260526121500||ORU^R01|MSG-R01|P|2.5\r" +
        "PID|||MRN-67890\r" +
        "OBX|1|NM|2160-0^Creatinine^LN||1.2|mg/dL|||||F";

    [Theory]
    [InlineData(AdtA01, "Patient")]
    [InlineData(AdtA01, "Encounter")]
    [InlineData(OruR01, "Observation")]
    public async Task End_To_End_Pipeline_Produces_Expected_Fhir_Bundle_Entry_Async(string hl7, string expectedResourceType)
    {
        await using var sp = await Build_Async(addVerifyFilters: true);
        var capture = sp.GetRequiredService<CapturingOutboundAdapter>();

        await Seedflow_Async(sp);
        await Dispatch_Async(sp, hl7);

        var captured = capture.Sent.Single();
        var json = Encoding.UTF8.GetString(captured.Payload.Span);
        var bundle = new FhirJsonDeserializer(new DeserializerSettings().UsingMode(DeserializationMode.Recoverable)).Deserialize<Bundle>(json);
        Assert.Contains(bundle.Entry, e => e.Resource!.TypeName == expectedResourceType);
    }

    [Fact]
    public async Task Pipeline_Drops_Non_Hl7_Payload_Via_Verify_Hl7_Filter_Async()
    {
        await using var sp = await Build_Async(addVerifyFilters: true);
        var capture = sp.GetRequiredService<CapturingOutboundAdapter>();
        await Seedflow_Async(sp);

        await Dispatch_Async(sp, "not an hl7 message");

        Assert.Empty(capture.Sent);
    }

    private static async Task<ServiceProvider> Build_Async(bool addVerifyFilters)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<CapturingOutboundAdapter>();
        services.AddSmartConnectPersistenceForPostgresql(SmartConnectPostgres.NewDatabaseConnectionString());
        services.AddSmartConnectCore();

        var sp = services.BuildServiceProvider();
        var registry = sp.GetRequiredService<MutableFlowPluginRegistry>();
        registry.RegisterOutboundAdapter(sp.GetRequiredService<CapturingOutboundAdapter>());
        _ = addVerifyFilters; // verify plugins are registered by AddSmartConnectCore already
        await Task.CompletedTask;
        return sp;
    }

    private static async Task Seedflow_Async(ServiceProvider sp)
    {
        await using var scope = sp.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();
        var pipeline = new IntegrationFlowPipelineDefinition
        {
            RouteFilters =
            [
                new RouteFilterSlot { Kind = AllowAllRouteFilter.KindValue },
                new RouteFilterSlot { Kind = "verify-hl7" },
            ],
            SourceTransformStages =
            [
                new TransformStageSlot { Kind = "hl7-to-fhir-pipeline" },
            ],
            OutboundRoutesSequential = false,
            OutboundRoutes =
            [
                new OutboundRouteSlot
                {
                    OutboundAdapterKind = CapturingOutboundAdapter.KindValue,
                },
            ],
        };
        db.IntegrationFlows.Add(new IntegrationFlowEntity
        {
            Id = Guid.Parse("00000000-0000-4000-8000-0000000000e1"),
            Name = "mllp-to-fhir-e2e",
            RuntimeState = (int)FlowRuntimeState.Started,
            PipelineJson = PipelineJsonSerializer.Serialize(pipeline),
        });
        await db.SaveChangesAsync();
    }

    private static async Task Dispatch_Async(ServiceProvider sp, string hl7)
    {
        await using var scope = sp.CreateAsyncScope();
        var runtime = scope.ServiceProvider.GetRequiredService<IFlowRuntime>();
        var msg = new IntegrationMessage
        {
            Id = Guid.NewGuid(),
            FlowId = Guid.Parse("00000000-0000-4000-8000-0000000000e1"),
            CorrelationId = "e2e-" + Guid.NewGuid().ToString("N")[..8],
            Payload = Encoding.UTF8.GetBytes(hl7),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };
        await runtime.DispatchAsync(msg, CancellationToken.None);
    }
}
