using Dialysis.SmartConnect.DataTypes;
using Dialysis.SmartConnect.Fhir;
using Dialysis.SmartConnect.Fhir.Mappers;
using Hl7.Fhir.Model;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Fhir;

/// <summary>
/// Dialysis Machine HL7 Implementation Guide rev 4.0 §6 (Reporting Treatment Information)
/// mandates the <c>ORU^R40^ORU_R40</c> trigger for PCD-01 messages. The existing
/// <c>OruR01ToObservationMapper</c> handled the older R01 trigger; this slice registers
/// the matching R40 mapper and verifies the pipeline routes both triggers through to a
/// FHIR <see cref="Observation"/>.
/// </summary>
public sealed class OruR40MapperTests
{
    private const string MinimalR40 =
        "MSH|^~\\&|ACME Dialysis Machine^00059AFFFE3C7A00^EUI-64|FAC|||" +
        "20260522140000||ORU^R40^ORU_R40|MSG-1|P|2.6\r" +
        "PID|||MRN-1\r" +
        "OBR|1\r" +
        "OBX|1|NM|29463-7^Body weight^LN||72.4|kg|||||F|||20260522140000\r";

    [Fact]
    public void Mapper_Advertises_Oru_R40_Trigger()
    {
        var mapper = new OruR40ToObservationMapper();
        Assert.Equal("ORU^R40", mapper.TriggerEvent);
    }

    [Fact]
    public void Pipeline_Dispatches_R40_To_R40_Mapper()
    {
        var pipeline = new Hl7V2ToFhirPipeline(
            new IFhirV2MessageMapperWrapper[]
            {
                new FhirMapperWrapper<Observation>(new OruR01ToObservationMapper()),
                new FhirMapperWrapper<Observation>(new OruR40ToObservationMapper()),
            });

        var produced = pipeline.Transform(Hl7V2Message.Parse(MinimalR40));

        // R40-only message → exactly one R40 observation; R01 mapper is filtered out by
        // trigger mismatch.
        var observation = Assert.IsType<Observation>(Assert.Single(produced));
        var coding = Assert.Single(observation.Code.Coding);
        Assert.Equal("29463-7", coding.Code);
        Assert.Equal("Body weight", coding.Display);
        var quantity = Assert.IsType<Quantity>(observation.Value);
        Assert.Equal(72.4m, quantity.Value);
        Assert.Equal("kg", quantity.Unit);
        Assert.Equal("Patient/MRN-1", observation.Subject?.Reference);
    }

    [Fact]
    public void Pipeline_Still_Routes_R01_When_Both_Mappers_Registered()
    {
        const string r01 =
            "MSH|^~\\&|SOURCE|FAC|||20260522140000||ORU^R01|MSG-2|P|2.6\r" +
            "PID|||MRN-2\r" +
            "OBX|1|NM|2823-3^Potassium^LN||5.1|mmol/L|||||F|||20260522140000\r";
        var pipeline = new Hl7V2ToFhirPipeline(
            new IFhirV2MessageMapperWrapper[]
            {
                new FhirMapperWrapper<Observation>(new OruR01ToObservationMapper()),
                new FhirMapperWrapper<Observation>(new OruR40ToObservationMapper()),
            });

        var produced = pipeline.Transform(Hl7V2Message.Parse(r01));

        // R01-only message → only the R01 mapper fires.
        var observation = Assert.IsType<Observation>(Assert.Single(produced));
        Assert.Equal("2823-3", observation.Code.Coding[0].Code);
    }

    /// <summary>
    /// Mirror of the internal <c>FhirV2MessageMapperWrapper&lt;TResource&gt;</c> used by the
    /// DI composition. Kept here as a test-only wrapper so we don't have to expose the
    /// internal type to the test assembly.
    /// </summary>
    private sealed class FhirMapperWrapper<TResource> : IFhirV2MessageMapperWrapper
        where TResource : Resource
    {
        private readonly IFhirV2MessageMapper<TResource> _inner;
        /// <summary>
        /// Mirror of the internal <c>FhirV2MessageMapperWrapper&lt;TResource&gt;</c> used by the
        /// DI composition. Kept here as a test-only wrapper so we don't have to expose the
        /// internal type to the test assembly.
        /// </summary>
        public FhirMapperWrapper(IFhirV2MessageMapper<TResource> inner) => _inner = inner;
        public string TriggerEvent => _inner.TriggerEvent;

        public Resource Map(Hl7V2Message message) => _inner.Map(message);
    }
}
