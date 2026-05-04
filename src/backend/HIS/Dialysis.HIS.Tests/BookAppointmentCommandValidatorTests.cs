using Dialysis.HIS.Scheduling.Features.BookAppointment;

namespace Dialysis.HIS.Tests;

public sealed class BookAppointmentCommandValidatorTests
{
    private readonly BookAppointmentCommandValidator _validator = new();

    [Fact]
    public async Task Rejects_empty_resource_kind()
    {
        var start = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var end = start.AddHours(1);
        var cmd = new BookAppointmentCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), start, end, "   ");
        var result = await _validator.ValidateAsync(cmd);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Rejects_when_end_before_start()
    {
        var start = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var end = start.AddHours(-1);
        var cmd = new BookAppointmentCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), start, end, "room");
        var result = await _validator.ValidateAsync(cmd);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Accepts_valid_interval()
    {
        var start = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var end = start.AddHours(1);
        var cmd = new BookAppointmentCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), start, end, "room");
        var result = await _validator.ValidateAsync(cmd);
        Assert.True(result.IsSuccess);
    }
}
