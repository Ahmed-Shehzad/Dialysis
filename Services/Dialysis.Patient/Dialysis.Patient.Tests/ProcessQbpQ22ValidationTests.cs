using Dialysis.Patient.Application.Features.ProcessQbpQ22Query;

using Shouldly;

namespace Dialysis.Patient.Tests;

/// <summary>
/// Edge-case and validation failure tests for ProcessQbpQ22QueryCommand.
/// </summary>
public sealed class ProcessQbpQ22ValidationTests
{
    private readonly ProcessQbpQ22QueryCommandValidator _validator = new();

    [Fact]
    public async Task Validate_EmptyRawHl7Message_FailsAsync()
    {
        var command = new ProcessQbpQ22QueryCommand("");
        Verifier.ValidationResult result = await _validator.ValidateAsync(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "RawHl7Message");
    }

    [Fact]
    public async Task Validate_WhitespaceOnlyRawHl7Message_FailsAsync()
    {
        var command = new ProcessQbpQ22QueryCommand("   ");
        Verifier.ValidationResult result = await _validator.ValidateAsync(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Validate_WrongMessageType_QbpD01_FailsAsync()
    {
        const string qbpD01 = """
            MSH|^~\&|MACH|FAC|EMR|FAC|20230215120000||QBP^D01^QBP_D01|MSG001|P|2.6
            QPD|MDC_HDIALY_RX_QUERY^X^MDC|Q001|@PID.3|MRN123^^^^MR
            RCP|I||RD
            """;

        var command = new ProcessQbpQ22QueryCommand(qbpD01);
        Verifier.ValidationResult result = await _validator.ValidateAsync(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage != null && e.ErrorMessage.Contains("QBP^Q22"));
    }

    [Fact]
    public async Task Validate_ValidQbpQ22_PassesAsync()
    {
        string qbpQ22 = PatientTestData.QbpQ22ByMrn("MRN123");
        var command = new ProcessQbpQ22QueryCommand(qbpQ22);
        Verifier.ValidationResult result = await _validator.ValidateAsync(command);

        result.IsValid.ShouldBeTrue();
    }
}
