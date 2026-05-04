using Dialysis.HIS.Medication.Domain;

namespace Dialysis.HIS.Tests;

public sealed class MedicationOrderDiscontinueTests
{
    [Fact]
    public void Discontinue_twice_throws()
    {
        var id = Guid.CreateVersion7();
        var patientId = Guid.CreateVersion7();
        var order = MedicationOrder.Place(id, patientId, "MED-1", DateTime.UtcNow, actorId: null);
        order.ClearIntegrationEvents();
        order.Discontinue(DateTime.UtcNow, actorId: null);
        Assert.Throws<InvalidOperationException>(() => order.Discontinue(DateTime.UtcNow, actorId: null));
    }

    [Fact]
    public void Discontinue_after_administration_throws()
    {
        var id = Guid.CreateVersion7();
        var patientId = Guid.CreateVersion7();
        var order = MedicationOrder.Place(id, patientId, "MED-1", DateTime.UtcNow, actorId: null);
        order.ClearIntegrationEvents();
        order.RecordAdministration(DateTime.UtcNow, actorId: null);
        Assert.Throws<InvalidOperationException>(() => order.Discontinue(DateTime.UtcNow, actorId: null));
    }

    [Fact]
    public void Administration_after_discontinue_throws()
    {
        var id = Guid.CreateVersion7();
        var patientId = Guid.CreateVersion7();
        var order = MedicationOrder.Place(id, patientId, "MED-1", DateTime.UtcNow, actorId: null);
        order.ClearIntegrationEvents();
        order.Discontinue(DateTime.UtcNow, actorId: null);
        Assert.Throws<InvalidOperationException>(() => order.RecordAdministration(DateTime.UtcNow, actorId: null));
    }
}
