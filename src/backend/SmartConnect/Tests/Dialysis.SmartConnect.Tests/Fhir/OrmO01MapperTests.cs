using Dialysis.SmartConnect.DataTypes;
using Dialysis.SmartConnect.Fhir;
using Dialysis.SmartConnect.Fhir.Mappers;
using Hl7.Fhir.Model;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Fhir;

/// <summary>
/// Covers the bi-directional-routing slice's ORM^O01 (general order message) mapper. Exercises the
/// pipeline trigger match and the minimum field set (LOINC code, order identifier, patient subject,
/// authored-on, status mapping from ORC-1).
/// </summary>
public sealed class OrmO01MapperTests
{
    private const string MinimalOrm =
        "MSH|^~\\&|EHR^123|FAC|LAB|FAC|20260601090000||ORM^O01|MSG-1|P|2.6\r" +
        "PID|||MRN-7\r" +
        "ORC|NW|ORD-001|||||||20260601090000\r" +
        "OBR|1||ORD-001|24323-8^Comprehensive metabolic panel^LN\r";

    [Fact]
    public void Mapper_Advertises_Orm_O01_Trigger()
    {
        var mapper = new OrmO01ToServiceRequestMapper();
        Assert.Equal("ORM^O01", mapper.TriggerEvent);
    }

    [Fact]
    public void Pipeline_Maps_Orm_To_Service_Request()
    {
        var pipeline = new Hl7V2ToFhirPipeline(
            new IFhirV2MessageMapperWrapper[]
            {
                new FhirMapperWrapper<ServiceRequest>(new OrmO01ToServiceRequestMapper()),
            });

        var produced = pipeline.Transform(Hl7V2Message.Parse(MinimalOrm));

        var request = Assert.IsType<ServiceRequest>(Assert.Single(produced));
        Assert.Equal(RequestStatus.Active, request.Status);
        Assert.Equal(RequestIntent.Order, request.Intent);
        Assert.Equal("24323-8", Assert.Single(request.Code!.Coding).Code);
        Assert.Equal("Patient/MRN-7", request.Subject?.Reference);
        Assert.Equal("ORD-001", Assert.Single(request.Identifier).Value);
        Assert.Equal("2026-06-01", request.AuthoredOn);
    }

    private sealed class FhirMapperWrapper<TResource>(IFhirV2MessageMapper<TResource> inner) : IFhirV2MessageMapperWrapper
        where TResource : Resource
    {
        public string TriggerEvent => inner.TriggerEvent;

        public Resource Map(Hl7V2Message message) => inner.Map(message);
    }
}
