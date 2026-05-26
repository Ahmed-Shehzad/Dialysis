using Dialysis.SmartConnect.DataTypes;
using Dialysis.SmartConnect.Fhir;
using Dialysis.SmartConnect.Fhir.Mappers;
using Hl7.Fhir.Model;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Fhir;

/// <summary>
/// Covers the bi-directional-routing slice's SIU^S12 (new appointment booking) mapper.
/// </summary>
public sealed class SiuS12MapperTests
{
    private const string MinimalSiu =
        "MSH|^~\\&|SCHED|FAC|EHR|FAC|20260601090000||SIU^S12|MSG-2|P|2.6\r" +
        "SCH|APPT-001||||||||30|min|^^30^20260602100000^20260602103000||||||||||||B\r" +
        "PID|||MRN-9\r";

    [Fact]
    public void Mapper_Advertises_Siu_S12_Trigger()
    {
        var mapper = new SiuS12ToAppointmentMapper();
        Assert.Equal("SIU^S12", mapper.TriggerEvent);
    }

    [Fact]
    public void Pipeline_Maps_Siu_To_Appointment()
    {
        var pipeline = new Hl7V2ToFhirPipeline(
            new IFhirV2MessageMapperWrapper[]
            {
                new FhirMapperWrapper<Appointment>(new SiuS12ToAppointmentMapper()),
            });

        var produced = pipeline.Transform(Hl7V2Message.Parse(MinimalSiu));

        var appointment = Assert.IsType<Appointment>(Assert.Single(produced));
        Assert.Equal(Appointment.AppointmentStatus.Booked, appointment.Status);
        Assert.Equal("APPT-001", Assert.Single(appointment.Identifier).Value);
        var participant = Assert.Single(appointment.Participant);
        Assert.Equal("Patient/MRN-9", participant.Actor?.Reference);
        Assert.Equal(new DateTimeOffset(2026, 6, 2, 10, 0, 0, TimeSpan.Zero), appointment.Start);
    }

    private sealed class FhirMapperWrapper<TResource>(IFhirV2MessageMapper<TResource> inner) : IFhirV2MessageMapperWrapper
        where TResource : Resource
    {
        public string TriggerEvent => inner.TriggerEvent;

        public Resource Map(Hl7V2Message message) => inner.Map(message);
    }
}
