using Dialysis.CQRS;
using Dialysis.HIS.Persistence;
using Dialysis.HIS.Scheduling.Features.BookAppointment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Dialysis.HIS.Tests;

[Collection(nameof(HisFixtureCollection))]
public sealed class SchedulingFlowTests
{
    private readonly HisApiWebApplicationFactory _factory;
    public SchedulingFlowTests(HisApiWebApplicationFactory factory) => _factory = factory;
    [Fact]
    public async Task Bookappointment_Persists_And_Enqueues_Outbox_Event_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();
        var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();

        var patientId = Guid.CreateVersion7();
        var providerId = Guid.CreateVersion7();
        var start = DateTime.UtcNow.AddDays(1);
        var id = await gateway.SendCommandAsync<BookAppointmentCommand, Guid>(
            new BookAppointmentCommand(patientId, providerId, start, start.AddMinutes(30)),
            CancellationToken.None);

        id.ShouldNotBe(Guid.Empty);

        var appt = await db.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, CancellationToken.None);
        appt.ShouldNotBeNull();
        appt.PatientId.ShouldBe(patientId);
        appt.StatusCode.ShouldBe("Booked");

        var outboxRow = await db.OutboxMessages.AsNoTracking()
            .Where(x => x.AssemblyQualifiedEventType.Contains("AppointmentBookedIntegrationEvent"))
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(CancellationToken.None);
        outboxRow.ShouldNotBeNull();
        outboxRow.PayloadJson.ShouldContain(id.ToString());
    }
}
