using Dialysis.BuildingBlocks.Verifier;
using Dialysis.HIS.Operations.Features.SubmitBillingExportJob;
using Shouldly;

namespace Dialysis.HIS.Tests;

public sealed class SubmitBillingExportJobValidatorTests
{
    private readonly SubmitBillingExportJobCommandValidator _sut = new();

    [Fact]
    public async Task Accepts_valid_command()
    {
        var cmd = new SubmitBillingExportJobCommand("AETNA-01", new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31), Notes: null);

        var result = await _sut.ValidateAsync(cmd, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("aetna")]
    [InlineData("a")]
    [InlineData("WAY-TOO-LONG-PAYER-NAME-12345")]
    [InlineData("ACME_01")]
    public async Task Rejects_invalid_payer_codes(string payerCode)
    {
        var cmd = new SubmitBillingExportJobCommand(payerCode, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31));

        var result = await _sut.ValidateAsync(cmd, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Rejects_period_start_not_before_end()
    {
        var cmd = new SubmitBillingExportJobCommand("AETNA", new DateOnly(2026, 5, 31), new DateOnly(2026, 5, 1));

        var result = await _sut.ValidateAsync(cmd, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Rejects_notes_over_500_chars()
    {
        var cmd = new SubmitBillingExportJobCommand(
            "AETNA",
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 31),
            Notes: new string('x', 501));

        var result = await _sut.ValidateAsync(cmd, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }
}
