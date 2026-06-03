using System.Text;
using Dialysis.SmartConnect.Fhir;
using Dialysis.SmartConnect.Fhir.Mappers;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.SmartConnect.Tests.Fhir;

/// <summary>
/// Covers the new <see cref="Hl7V2ToFhirTransformStage"/>: feeds an HL7 v2 payload through the
/// stage and asserts the output is a FHIR Bundle containing the right resources.
/// </summary>
public sealed class Hl7V2ToFhirTransformStageTests
{
    private static Hl7V2ToFhirTransformStage BuildStage()
    {
        var pipeline = new Hl7V2ToFhirPipeline(new IFhirV2MessageMapperWrapper[]
        {
            new MapperWrapper<Patient>(new AdtA01ToPatientMapper()),
            new MapperWrapper<Encounter>(new AdtA01ToEncounterMapper()),
            new MapperWrapper<Observation>(new OruR01ToObservationMapper()),
        });
        return new Hl7V2ToFhirTransformStage(pipeline);
    }

    [Fact]
    public async Task Transform_Oru_R01_Emits_Bundle_With_Observation_Async()
    {
        var stage = BuildStage();
        var raw = "MSH|^~\\&|LAB|HOSPITAL|EMR|CLINIC|20260526121500||ORU^R01|MSG-1|P|2.5\r" +
                  "PID|||MRN-12345\r" +
                  "OBX|1|NM|2160-0^Creatinine^LN||1.2|mg/dL|||||F";

        var input = NewMessage(raw);
        var output = await stage.TransformAsync(input, CancellationToken.None);
        var bundle = DeserializeBundle(output.Payload);

        Assert.Equal(Bundle.BundleType.Collection, bundle.Type);
        var observation = Assert.IsType<Observation>(Assert.Single(bundle.Entry).Resource);
        Assert.Equal("2160-0", Assert.Single(observation.Code.Coding).Code);
        Assert.Equal("Patient/MRN-12345", observation.Subject?.Reference);
    }

    [Fact]
    public async Task Transform_Adt_A01_Emits_Bundle_With_Patient_And_Encounter_Async()
    {
        var stage = BuildStage();
        var raw = "MSH|^~\\&|HIS|HOSPITAL|EMR|CLINIC|20260526120000||ADT^A01|MSG-2|P|2.5\r" +
                  "EVN|A01|20260526120000\r" +
                  "PID|||MRN-67890||DOE^JANE\r" +
                  "PV1|1|I|ICU^101^A";

        var input = NewMessage(raw);
        var output = await stage.TransformAsync(input, CancellationToken.None);
        var bundle = DeserializeBundle(output.Payload);

        Assert.Equal(2, bundle.Entry.Count);
        Assert.Contains(bundle.Entry, e => e.Resource is Patient);
        Assert.Contains(bundle.Entry, e => e.Resource is Encounter);
    }

    [Fact]
    public async Task Transform_Non_Hl7_Payload_Passes_Through_Unchanged_Async()
    {
        var stage = BuildStage();
        var input = NewMessage("not an hl7 message — passes through");

        var output = await stage.TransformAsync(input, CancellationToken.None);

        Assert.Equal(input.Payload.ToArray(), output.Payload.ToArray());
    }

    [Fact]
    public async Task Transform_With_No_Matching_Mapper_Passes_Through_Async()
    {
        var stage = BuildStage();
        // SIU^S12 mapper not registered in this test's pipeline composition.
        var raw = "MSH|^~\\&|SCHED|CLINIC|EHR|HOSPITAL|20260526100000||SIU^S12|MSG-3|P|2.5\r" +
                  "PID|||MRN-99999";

        var input = NewMessage(raw);
        var output = await stage.TransformAsync(input, CancellationToken.None);

        Assert.Equal(input.Payload.ToArray(), output.Payload.ToArray());
    }

    private static IntegrationMessage NewMessage(string text) => new()
    {
        Id = Guid.CreateVersion7(),
        FlowId = Guid.CreateVersion7(),
        CorrelationId = "corr-" + Guid.NewGuid().ToString("N")[..8],
        Payload = Encoding.UTF8.GetBytes(text),
        PayloadFormat = PayloadFormat.Utf8Text,
        ReceivedAtUtc = DateTimeOffset.UtcNow,
    };

    private static Bundle DeserializeBundle(ReadOnlyMemory<byte> payload)
    {
        var json = Encoding.UTF8.GetString(payload.Span);
        var parser = new FhirJsonDeserializer(new DeserializerSettings().UsingMode(DeserializationMode.Recoverable));
        return parser.Deserialize<Bundle>(json);
    }

    private sealed class MapperWrapper<TResource>(IFhirV2MessageMapper<TResource> inner) : IFhirV2MessageMapperWrapper
        where TResource : Resource
    {
        public string TriggerEvent => inner.TriggerEvent;

        public Resource Map(Dialysis.SmartConnect.DataTypes.Hl7V2Message message) => inner.Map(message);
    }
}
