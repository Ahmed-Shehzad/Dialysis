using Dialysis.HIS.Integration.Features.IngestDeviceReading;
using Shouldly;

namespace Dialysis.HIS.Tests;

public sealed class IngestDeviceReadingValidatorTests
{
    private readonly IngestDeviceReadingCommandValidator _sut = new();

    [Fact]
    public async Task Accepts_Valid_Command_Async()
    {
        var cmd = new IngestDeviceReadingCommand("device-01", Guid.NewGuid(), """{"bp":120}""");

        var result = await _sut.ValidateAsync(cmd, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Rejects_Empty_Device_Id_Async()
    {
        var cmd = new IngestDeviceReadingCommand("  ", Guid.NewGuid(), """{"bp":120}""");

        (await _sut.ValidateAsync(cmd, CancellationToken.None)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Rejects_Empty_Patient_Id_Async()
    {
        var cmd = new IngestDeviceReadingCommand("device-01", Guid.Empty, """{"bp":120}""");

        (await _sut.ValidateAsync(cmd, CancellationToken.None)).IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{unterminated")]
    public async Task Rejects_Malformed_Payload_Async(string payload)
    {
        var cmd = new IngestDeviceReadingCommand("device-01", Guid.NewGuid(), payload);

        (await _sut.ValidateAsync(cmd, CancellationToken.None)).IsFailure.ShouldBeTrue();
    }
}
