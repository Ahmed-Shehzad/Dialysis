using Dialysis.SmartConnect.DataTypes;
using Dialysis.SmartConnect.Fhir;
using Dialysis.SmartConnect.Fhir.Mappers;
using Hl7.Fhir.Model;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Fhir;

/// <summary>
/// Covers the bi-directional-routing slice's VXU^V04 (unsolicited vaccination record update) mapper.
/// </summary>
public sealed class VxuV04MapperTests
{
    private const string MinimalVxu =
        "MSH|^~\\&|VAX|FAC|REGISTRY|FAC|20260601090000||VXU^V04|MSG-4|P|2.6\r" +
        "PID|||MRN-11\r" +
        "RXA|0|1|20260101120000||207^COVID-19 mRNA vaccine^CVX|0.3|||||||||LOT-A1\r";

    [Fact]
    public void Mapper_Advertises_Vxu_V04_Trigger()
    {
        var mapper = new VxuV04ToImmunizationMapper();
        Assert.Equal("VXU^V04", mapper.TriggerEvent);
    }

    [Fact]
    public void Pipeline_Maps_Vxu_To_Immunization()
    {
        var pipeline = new Hl7V2ToFhirPipeline(
            new IFhirV2MessageMapperWrapper[]
            {
                new FhirMapperWrapper<Immunization>(new VxuV04ToImmunizationMapper()),
            });

        var produced = pipeline.Transform(Hl7V2Message.Parse(MinimalVxu));

        var imm = Assert.IsType<Immunization>(Assert.Single(produced));
        Assert.Equal(Immunization.ImmunizationStatusCodes.Completed, imm.Status);
        Assert.Equal("207", Assert.Single(imm.VaccineCode.Coding).Code);
        Assert.Equal("Patient/MRN-11", imm.Patient?.Reference);
        Assert.Equal("LOT-A1", imm.LotNumber);
        var dateTime = Assert.IsType<FhirDateTime>(imm.Occurrence);
        Assert.StartsWith("2026-01-01", dateTime.Value);
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
