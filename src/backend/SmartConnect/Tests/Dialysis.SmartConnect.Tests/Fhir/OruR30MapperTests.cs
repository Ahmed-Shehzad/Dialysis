using Dialysis.SmartConnect.DataTypes;
using Dialysis.SmartConnect.Fhir;
using Dialysis.SmartConnect.Fhir.Mappers;
using Hl7.Fhir.Model;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Fhir;

/// <summary>
/// Covers ORU^R30 (unsolicited point-of-care observation). Same OBX shape as R01 but a different
/// trigger so downstream consumers can apply POC-specific validation / retention policies.
/// </summary>
public sealed class OruR30MapperTests
{
    private const string OruR30 =
        "MSH|^~\\&|DEVICE|FAC|EHR|FAC|20260601121500||ORU^R30|MSG-R30|P|2.6\r" +
        "PID|||MRN-R30\r" +
        "OBX|1|NM|2160-0^Creatinine^LN||1.2|mg/dL|||||F|||20260601121500\r";

    [Fact]
    public void Mapper_Advertises_Oru_R30_Trigger()
    {
        var mapper = new OruR30ToObservationMapper();
        Assert.Equal("ORU^R30", mapper.TriggerEvent);
    }

    [Fact]
    public void Pipeline_Maps_R30_To_Observation_With_Poc_Tag()
    {
        var pipeline = new Hl7V2ToFhirPipeline(new IFhirV2MessageMapperWrapper[]
        {
            new FhirMapperWrapper<Observation>(new OruR30ToObservationMapper()),
        });

        var produced = pipeline.Transform(Hl7V2Message.Parse(OruR30));

        var observation = Assert.IsType<Observation>(Assert.Single(produced));
        Assert.Equal("2160-0", Assert.Single(observation.Code.Coding).Code);
        Assert.Equal("Patient/MRN-R30", observation.Subject?.Reference);
        var quantity = Assert.IsType<Quantity>(observation.Value);
        Assert.Equal(1.2m, quantity.Value);
        Assert.Contains(observation.Meta?.Tag ?? [], t => t.Code == "POC");
    }

    private sealed class FhirMapperWrapper<TResource> : IFhirV2MessageMapperWrapper
        where TResource : Resource
    {
        private readonly IFhirV2MessageMapper<TResource> _inner;
        public FhirMapperWrapper(IFhirV2MessageMapper<TResource> inner) => _inner = inner;
        public string TriggerEvent => _inner.TriggerEvent;

        public Resource Map(Hl7V2Message message) => _inner.Map(message);
    }
}
