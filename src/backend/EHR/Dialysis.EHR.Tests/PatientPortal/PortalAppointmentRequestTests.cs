using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.EHR.PatientPortal.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.PatientPortal;

public sealed class PortalAppointmentRequestTests
{
    private static readonly DateTime _earliest = DateTime.UtcNow.AddDays(2);
    private static readonly DateTime _latest = DateTime.UtcNow.AddDays(9);

    private static PortalAppointmentRequest Pending()
    {
        var request = PortalAppointmentRequest.Submit(
            Guid.NewGuid(), Guid.NewGuid(), "Fistula check", _earliest, _latest);
        request.ClearIntegrationEvents();
        return request;
    }

    [Fact]
    public void Approve_Links_The_Appointment_And_Raises_Resolved()
    {
        var request = Pending();
        var appointmentId = Guid.NewGuid();

        request.Approve(appointmentId, "Booked Tuesday 9am");

        request.Status.ShouldBe(PortalAppointmentRequestStatus.Approved);
        request.CreatedAppointmentId.ShouldBe(appointmentId);
        var raised = request.IntegrationEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<PatientPortalAppointmentResolvedIntegrationEvent>();
        raised.Approved.ShouldBeTrue();
        raised.CreatedAppointmentId.ShouldBe(appointmentId);
    }

    [Fact]
    public void Decline_Raises_Resolved_Not_Approved()
    {
        var request = Pending();

        request.Decline("No capacity this week");

        request.Status.ShouldBe(PortalAppointmentRequestStatus.Declined);
        request.IntegrationEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<PatientPortalAppointmentResolvedIntegrationEvent>()
            .Approved.ShouldBeFalse();
    }

    [Fact]
    public void Cannot_Approve_A_Non_Pending_Request()
    {
        var request = Pending();
        request.Cancel();

        Should.Throw<DomainException>(() => request.Approve(Guid.NewGuid()));
    }
}
