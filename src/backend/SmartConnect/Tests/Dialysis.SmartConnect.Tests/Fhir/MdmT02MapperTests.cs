using System.Text;
using Dialysis.SmartConnect.DataTypes;
using Dialysis.SmartConnect.Fhir;
using Dialysis.SmartConnect.Fhir.Mappers;
using Hl7.Fhir.Model;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Fhir;

/// <summary>
/// Covers the bi-directional-routing slice's MDM^T02 (medical document notification) mapper.
/// </summary>
public sealed class MdmT02MapperTests
{
    private const string MinimalMdm =
        "MSH|^~\\&|EHR|FAC|HIE|FAC|20260601090000||MDM^T02|MSG-3|P|2.6\r" +
        "EVN|T02|20260601090000\r" +
        "PID|||MRN-3\r" +
        "TXA|1|11506-3^Progress note^LN|||20260601090000|||||||DOC-123\r" +
        "OBX|1|TX|11506-3^Progress note^LN||Patient stable, continue current regimen.|||||||F\r";

    [Fact]
    public void Mapper_Advertises_Mdm_T02_Trigger()
    {
        var mapper = new MdmT02ToDocumentReferenceMapper();
        Assert.Equal("MDM^T02", mapper.TriggerEvent);
    }

    [Fact]
    public void Pipeline_Maps_Mdm_To_Document_Reference()
    {
        var pipeline = new Hl7V2ToFhirPipeline(
            new IFhirV2MessageMapperWrapper[]
            {
                new FhirMapperWrapper<DocumentReference>(new MdmT02ToDocumentReferenceMapper()),
            });

        var produced = pipeline.Transform(Hl7V2Message.Parse(MinimalMdm));

        var doc = Assert.IsType<DocumentReference>(Assert.Single(produced));
        Assert.Equal(DocumentReferenceStatus.Current, doc.Status);
        Assert.Equal("11506-3", Assert.Single(doc.Type!.Coding).Code);
        Assert.Equal("Patient/MRN-3", doc.Subject?.Reference);
        Assert.Equal("DOC-123", Assert.Single(doc.Identifier).Value);
        var content = Assert.Single(doc.Content);
        Assert.Equal("text/plain", content.Attachment.ContentType);
        Assert.Equal(
            "Patient stable, continue current regimen.",
            Encoding.UTF8.GetString(content.Attachment.Data!));
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
