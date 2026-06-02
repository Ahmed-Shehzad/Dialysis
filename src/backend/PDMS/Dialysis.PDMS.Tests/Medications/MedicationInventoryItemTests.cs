using Dialysis.PDMS.Medications.Contracts;
using Dialysis.PDMS.Medications.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.Medications;

public sealed class MedicationInventoryItemTests
{
    [Fact]
    public void Deducting_Below_Threshold_Raises_Low_Event()
    {
        var item = Build_Item(onHand: 12, threshold: 10);

        var result = item.Deduct(units: 5, reason: "session:abc");

        result.ShouldBe(7);
        item.IntegrationEvents.OfType<MedicationInventoryLowIntegrationEvent>()
            .ShouldHaveSingleItem()
            .OnHandUnits.ShouldBe(7);
    }

    [Fact]
    public void Deducting_Above_Threshold_Does_Not_Raise()
    {
        var item = Build_Item(onHand: 100, threshold: 10);
        item.Deduct(5, reason: "session:abc");
        item.IntegrationEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Receive_Increases_On_Hand()
    {
        var item = Build_Item(onHand: 10, threshold: 5);
        item.Receive(20, "PO #1234");
        item.OnHandUnits.ShouldBe(30);
    }

    [Fact]
    public void Adjust_Sets_New_Total_Bypassing_Threshold_Event()
    {
        var item = Build_Item(onHand: 100, threshold: 10);
        item.Adjust(8, "Physical count");
        item.OnHandUnits.ShouldBe(8);
        // Adjust by spec doesn't raise the low event — physical count is operator-initiated.
        item.IntegrationEvents.OfType<MedicationInventoryLowIntegrationEvent>().ShouldBeEmpty();
    }

    [Fact]
    public void Deducting_More_Than_On_Hand_Goes_Negative_Without_Throwing()
    {
        var item = Build_Item(onHand: 3, threshold: 1);
        var result = item.Deduct(5, "session:abc");
        result.ShouldBe(-2);
        // Goes below threshold so the event fires.
        item.IntegrationEvents.OfType<MedicationInventoryLowIntegrationEvent>().ShouldHaveSingleItem();
    }

    private static MedicationInventoryItem Build_Item(int onHand, int threshold) =>
        new(
            id: Guid.CreateVersion7(),
            medication: MedicationCoding.RxNorm("1234", "Heparin"),
            lotNumber: "LOT-2026-001",
            expiryUtc: DateTime.UtcNow.AddYears(1),
            initialOnHand: onHand,
            threshold: threshold);
}
