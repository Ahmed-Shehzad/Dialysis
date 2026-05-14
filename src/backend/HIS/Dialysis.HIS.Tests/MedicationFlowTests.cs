using Dialysis.CQRS;
using Dialysis.HIS.Medication.Features.PlaceMedicationOrder;
using Dialysis.HIS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Dialysis.HIS.Tests;

[Collection(nameof(HisFixtureCollection))]
public sealed class MedicationFlowTests(HisApiWebApplicationFactory factory)
{
    [Fact]
    public async Task Placemedicationorder_Persists_Order_And_Enqueues_Event_Async()
    {
        using var scope = factory.Services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();
        var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();

        var patientId = Guid.CreateVersion7();
        var id = await gateway.SendCommandAsync<PlaceMedicationOrderCommand, Guid>(
            new PlaceMedicationOrderCommand(patientId, "ASPIRIN-100", "100mg PO daily"),
            CancellationToken.None);

        var order = await db.MedicationOrders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, CancellationToken.None);
        order.ShouldNotBeNull();
        order.PatientId.ShouldBe(patientId);
        order.DrugCode.Value.ShouldBe("ASPIRIN-100");
        order.StatusCode.ShouldBe("Placed");

        var outboxRow = await db.OutboxMessages.AsNoTracking()
            .Where(x => x.AssemblyQualifiedEventType.Contains("MedicationOrderPlacedIntegrationEvent"))
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(CancellationToken.None);
        outboxRow.ShouldNotBeNull();
        outboxRow.PayloadJson.ShouldContain(id.ToString());
    }
}
