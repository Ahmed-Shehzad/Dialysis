using Dialysis.Lab.Contracts;
using Dialysis.Lab.Orders.Features.PlaceLabOrder;
using Shouldly;
using Xunit;

namespace Dialysis.Lab.Tests;

public sealed class PlaceLabOrderValidatorTests
{
    private readonly PlaceLabOrderCommandValidator _sut = new();

    private static IReadOnlyList<LabTestRequestContract> OneTest() =>
        [new LabTestRequestContract("718-7", "Hemoglobin")];

    [Fact]
    public async Task Accepts_A_Valid_Command_Async()
    {
        var cmd = new PlaceLabOrderCommand(Guid.NewGuid(), OneTest(), LabOrderPriority.Routine, "Serum", "dr.grey");

        (await _sut.ValidateAsync(cmd, CancellationToken.None)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Rejects_An_Empty_Patient_Async()
    {
        var cmd = new PlaceLabOrderCommand(Guid.Empty, OneTest(), LabOrderPriority.Routine, null, "dr.grey");

        (await _sut.ValidateAsync(cmd, CancellationToken.None)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Rejects_No_Tests_Async()
    {
        var cmd = new PlaceLabOrderCommand(Guid.NewGuid(), [], LabOrderPriority.Routine, null, "dr.grey");

        (await _sut.ValidateAsync(cmd, CancellationToken.None)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Rejects_A_Test_With_A_Blank_Loinc_Async()
    {
        var cmd = new PlaceLabOrderCommand(
            Guid.NewGuid(), [new LabTestRequestContract("", "Hemoglobin")], LabOrderPriority.Routine, null, "dr.grey");

        (await _sut.ValidateAsync(cmd, CancellationToken.None)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Rejects_A_Blank_Placed_By_Async()
    {
        var cmd = new PlaceLabOrderCommand(Guid.NewGuid(), OneTest(), LabOrderPriority.Routine, null, "  ");

        (await _sut.ValidateAsync(cmd, CancellationToken.None)).IsFailure.ShouldBeTrue();
    }
}
