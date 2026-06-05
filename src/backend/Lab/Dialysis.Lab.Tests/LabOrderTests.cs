using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.Lab.Contracts;
using Dialysis.Lab.Contracts.IntegrationEvents;
using Dialysis.Lab.Orders.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.Lab.Tests;

public sealed class LabOrderTests
{
    private static readonly Guid _patient = Guid.NewGuid();
    private static readonly IReadOnlyList<LabTestItem> _tests =
        [new LabTestItem("718-7", "Hemoglobin"), new LabTestItem("2160-0", "Creatinine")];

    private static LabOrder Place() =>
        LabOrder.Place(_patient, _tests, LabOrderPriority.Routine, "Serum", "dr.grey", DateTime.UtcNow);

    [Fact]
    public void Place_Creates_A_Placed_Order_And_Raises_The_Event()
    {
        var order = Place();

        order.Status.ShouldBe(LabOrderStatus.Placed);
        order.PatientId.ShouldBe(_patient);
        order.PlacerOrderNumber.ShouldStartWith("LAB-");
        order.Tests.Count.ShouldBe(2);

        var evt = order.IntegrationEvents.OfType<LabOrderPlacedIntegrationEvent>().ShouldHaveSingleItem();
        evt.SchemaVersion.ShouldBe(1);
        evt.PlacerOrderNumber.ShouldBe(order.PlacerOrderNumber);
        evt.Tests.Count.ShouldBe(2);
    }

    [Fact]
    public void Place_Rejects_An_Empty_Patient() =>
        Should.Throw<DomainException>(() =>
            LabOrder.Place(Guid.Empty, _tests, LabOrderPriority.Routine, null, "dr.grey", DateTime.UtcNow));

    [Fact]
    public void Place_Rejects_An_Order_With_No_Tests() =>
        Should.Throw<DomainException>(() =>
            LabOrder.Place(_patient, [], LabOrderPriority.Routine, null, "dr.grey", DateTime.UtcNow));

    [Fact]
    public void Mark_Transmitted_Only_From_Placed()
    {
        var order = Place();
        order.MarkTransmitted("FILL-1");
        order.Status.ShouldBe(LabOrderStatus.Transmitted);
        order.FillerOrderNumber.ShouldBe("FILL-1");

        Should.Throw<DomainException>(() => order.MarkTransmitted("FILL-2"));
    }

    [Fact]
    public void Record_Results_Completes_The_Order()
    {
        var order = Place();
        order.RecordResults(
            [new LabResultItem("718-7", "Hemoglobin", "9.1", "g/dL", "13.5-17.5", LabResultInterpretation.Low)],
            "FILL-1",
            DateTime.UtcNow);

        order.Status.ShouldBe(LabOrderStatus.Resulted);
        order.ResultedAtUtc.ShouldNotBeNull();
        order.Results.ShouldHaveSingleItem().Interpretation.ShouldBe(LabResultInterpretation.Low);
    }

    [Fact]
    public void Cancel_Is_Rejected_After_Resulted()
    {
        var order = Place();
        order.RecordResults([new LabResultItem("718-7", "Hemoglobin", "14", "g/dL", null, LabResultInterpretation.Normal)], null, DateTime.UtcNow);

        Should.Throw<DomainException>(order.Cancel);
    }
}
