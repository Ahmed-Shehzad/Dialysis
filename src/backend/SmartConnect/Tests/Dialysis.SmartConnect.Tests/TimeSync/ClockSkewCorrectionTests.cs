using Dialysis.SmartConnect.DataTypes;
using Dialysis.SmartConnect.TimeSync;
using Xunit;

namespace Dialysis.SmartConnect.Tests.TimeSync;

/// <summary>
/// Slice J of the SmartConnect ↔ Mirth alignment plan: the §2 clock-skew probe optionally
/// rewrites <c>MSH-7</c> to the server clock when drift exceeds a configured threshold and
/// is still within a safety bound. Each test exercises one branch of the
/// <see cref="Hl7V2ClockSkewProbe.TryObserveAndCorrect"/> decision tree.
/// </summary>
public sealed class ClockSkewCorrectionTests
{
    private const string SampleMsh =
        "MSH|^~\\&|MachineA^00059AFFFE3C7A00^EUI-64|FACILITY|||" +
        "{TS}||ORU^R40^ORU_R40|MSG-1|P|2.6\r" +
        "PID|||MRN-1\r";

    [Fact]
    public void Report_Only_Mode_Records_Observation_And_Leaves_Message_Untouched()
    {
        var message = Hl7V2Message.Parse(SampleMsh.Replace("{TS}", "20260522135900"));
        var monitor = new InMemoryClockSkewMonitor();
        // 59s drift — large enough to be classified as 'alert' by the existing severity rule.
        var serverNow = new DateTime(2026, 5, 22, 13, 59, 59, DateTimeKind.Utc);

        var result = Hl7V2ClockSkewProbe.TryObserveAndCorrect(
            message, serverNow, monitor, ClockSkewCorrectionPolicy.ReportOnly);

        Assert.NotNull(result);
        Assert.False(result!.WasCorrected);
        Assert.Null(result.CorrectedMessageTimestampUtc);
        Assert.Equal("20260522135900", message.GetValue("MSH.7"));
        // Monitor still sees the original skew so the operator dashboard shows reality.
        Assert.Equal("alert", Assert.Single(monitor.List()).Severity);
    }

    [Fact]
    public void Normalize_Mode_Rewrites_Msh_7_When_Skew_Exceeds_Threshold()
    {
        var message = Hl7V2Message.Parse(SampleMsh.Replace("{TS}", "20260522135900"));
        var monitor = new InMemoryClockSkewMonitor();
        var serverNow = new DateTime(2026, 5, 22, 13, 59, 59, DateTimeKind.Utc);
        var policy = ClockSkewCorrectionPolicy.Normalize(
            correctAbove: TimeSpan.FromSeconds(1),
            maxAllowed: TimeSpan.FromHours(1));

        var result = Hl7V2ClockSkewProbe.TryObserveAndCorrect(message, serverNow, monitor, policy);

        Assert.NotNull(result);
        Assert.True(result!.WasCorrected);
        Assert.Equal(serverNow, result.CorrectedMessageTimestampUtc);
        Assert.Equal("20260522135959", message.GetValue("MSH.7"));
        // Monitor sees the *original* 59s skew, not the corrected zero.
        var status = Assert.Single(monitor.List());
        Assert.InRange(status.LastSkew.TotalSeconds, 58, 60);
    }

    [Fact]
    public void Normalize_Mode_Leaves_Sub_Threshold_Skew_Alone()
    {
        var message = Hl7V2Message.Parse(SampleMsh.Replace("{TS}", "20260522135959"));
        var monitor = new InMemoryClockSkewMonitor();
        // 500 ms drift — within the IHE Consistent Time tolerance, not worth retiming.
        var serverNow = new DateTime(2026, 5, 22, 13, 59, 59, 500, DateTimeKind.Utc);
        var policy = ClockSkewCorrectionPolicy.Normalize(
            correctAbove: TimeSpan.FromSeconds(1),
            maxAllowed: TimeSpan.FromHours(1));

        var result = Hl7V2ClockSkewProbe.TryObserveAndCorrect(message, serverNow, monitor, policy);

        Assert.NotNull(result);
        Assert.False(result!.WasCorrected);
        Assert.Equal("below correction threshold", result.RejectionReason);
        Assert.Equal("20260522135959", message.GetValue("MSH.7"));
    }

    [Fact]
    public void Normalize_Mode_Refuses_To_Correct_Catastrophic_Skew()
    {
        // Message dated 1970 epoch (machine post-reset) but server is in 2026 — silently
        // rewriting that to "now" would mask a real misconfiguration. The policy's
        // MaxAllowedAbsJump is the circuit breaker.
        var message = Hl7V2Message.Parse(SampleMsh.Replace("{TS}", "19700101000000"));
        var monitor = new InMemoryClockSkewMonitor();
        var serverNow = new DateTime(2026, 5, 22, 13, 59, 59, DateTimeKind.Utc);
        var policy = ClockSkewCorrectionPolicy.Normalize(
            correctAbove: TimeSpan.FromSeconds(1),
            maxAllowed: TimeSpan.FromHours(1));

        var result = Hl7V2ClockSkewProbe.TryObserveAndCorrect(message, serverNow, monitor, policy);

        Assert.NotNull(result);
        Assert.False(result!.WasCorrected);
        Assert.Equal("exceeds MaxAllowedAbsJump", result.RejectionReason);
        Assert.Equal("19700101000000", message.GetValue("MSH.7"));
        // The monitor still records the absurd skew so an operator sees the alert.
        Assert.Equal("alert", Assert.Single(monitor.List()).Severity);
    }

    [Fact]
    public void Returns_Null_When_Msh_7_Is_Missing()
    {
        var message = Hl7V2Message.Parse(
            "MSH|^~\\&|MachineA|FACILITY|||||ORU^R40^ORU_R40|MSG-1|P|2.6\r");
        var monitor = new InMemoryClockSkewMonitor();
        var policy = ClockSkewCorrectionPolicy.Normalize(
            correctAbove: TimeSpan.FromSeconds(1),
            maxAllowed: TimeSpan.FromHours(1));

        var result = Hl7V2ClockSkewProbe.TryObserveAndCorrect(
            message, DateTime.UtcNow, monitor, policy);

        Assert.Null(result);
        Assert.Empty(monitor.List());
    }
}
