using Dialysis.SmartConnect.DataTypes;
using Dialysis.SmartConnect.Fhir;
using Dialysis.SmartConnect.Fhir.Mappers;
using Hl7.Fhir.Model;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Fhir;

/// <summary>
/// Covers the additional ADT trigger variants added by the bi-directional-routing slice: A04
/// (register), A08 (update), A40 (merge). A01 stays covered by its own existing tests; these
/// verify the new mappers advertise the right trigger and behave consistently.
/// </summary>
public sealed class AdtVariantMapperTests
{
    private const string AdtA04 =
        "MSH|^~\\&|REG|FAC|EHR|FAC|20260601090000||ADT^A04|MSG-A04|P|2.6\r" +
        "PID|||MRN-A04||Doe^Jane||19850105|F\r";

    private const string AdtA08 =
        "MSH|^~\\&|REG|FAC|EHR|FAC|20260601090000||ADT^A08|MSG-A08|P|2.6\r" +
        "PID|||MRN-A08||Smith^John||19720420|M\r";

    private const string AdtA40 =
        "MSH|^~\\&|REG|FAC|EHR|FAC|20260601090000||ADT^A40|MSG-A40|P|2.6\r" +
        "PID|||MRN-SURVIVING||Doe^Jane\r" +
        "MRG|MRN-OLD\r";

    [Fact]
    public void A04_Mapper_Maps_To_Patient()
    {
        var pipeline = new Hl7V2ToFhirPipeline(new IFhirV2MessageMapperWrapper[]
        {
            new FhirMapperWrapper<Patient>(new AdtA04ToPatientMapper()),
        });

        var produced = pipeline.Transform(Hl7V2Message.Parse(AdtA04));

        var patient = Assert.IsType<Patient>(Assert.Single(produced));
        Assert.Equal("MRN-A04", patient.Id);
        Assert.Equal(AdministrativeGender.Female, patient.Gender);
        Assert.Equal("Doe", Assert.Single(patient.Name).Family);
    }

    [Fact]
    public void A08_Mapper_Maps_To_Patient_And_Tags_Update()
    {
        var pipeline = new Hl7V2ToFhirPipeline(new IFhirV2MessageMapperWrapper[]
        {
            new FhirMapperWrapper<Patient>(new AdtA08ToPatientMapper()),
        });

        var produced = pipeline.Transform(Hl7V2Message.Parse(AdtA08));

        var patient = Assert.IsType<Patient>(Assert.Single(produced));
        Assert.Equal("MRN-A08", patient.Id);
        Assert.Equal(AdministrativeGender.Male, patient.Gender);
        Assert.Contains(patient.Meta?.Tag ?? [], t => t.Code == "patient-update");
    }

    [Fact]
    public void A40_Mapper_Maps_Merge_Link()
    {
        var pipeline = new Hl7V2ToFhirPipeline(new IFhirV2MessageMapperWrapper[]
        {
            new FhirMapperWrapper<Patient>(new AdtA40ToPatientMapper()),
        });

        var produced = pipeline.Transform(Hl7V2Message.Parse(AdtA40));

        var patient = Assert.IsType<Patient>(Assert.Single(produced));
        Assert.Equal("MRN-SURVIVING", patient.Id);
        var link = Assert.Single(patient.Link);
        Assert.Equal("Patient/MRN-OLD", link.Other?.Reference);
        Assert.Equal(Patient.LinkType.Replaces, link.Type);
    }

    [Fact]
    public void Pipeline_Routes_By_Trigger_Across_All_Adt_Variants()
    {
        var pipeline = new Hl7V2ToFhirPipeline(new IFhirV2MessageMapperWrapper[]
        {
            new FhirMapperWrapper<Patient>(new AdtA01ToPatientMapper()),
            new FhirMapperWrapper<Patient>(new AdtA04ToPatientMapper()),
            new FhirMapperWrapper<Patient>(new AdtA08ToPatientMapper()),
            new FhirMapperWrapper<Patient>(new AdtA40ToPatientMapper()),
        });

        // A04 message + all 4 mappers registered → only A04 fires (single result).
        var produced = pipeline.Transform(Hl7V2Message.Parse(AdtA04));
        var patient = Assert.IsType<Patient>(Assert.Single(produced));
        Assert.Equal("MRN-A04", patient.Id);
    }

    private sealed class FhirMapperWrapper<TResource>(IFhirV2MessageMapper<TResource> inner) : IFhirV2MessageMapperWrapper
        where TResource : Resource
    {
        public string TriggerEvent => inner.TriggerEvent;

        public Resource Map(Hl7V2Message message) => inner.Map(message);
    }
}
