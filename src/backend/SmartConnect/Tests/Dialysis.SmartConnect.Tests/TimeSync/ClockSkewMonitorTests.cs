using Dialysis.SmartConnect.DataTypes;
using Dialysis.SmartConnect.TimeSync;
using Xunit;

namespace Dialysis.SmartConnect.Tests.TimeSync;

/// <summary>
/// Covers the §2 Time Synchronization probe end-to-end: parse MSH-7 off an inbound
/// HL7v2 message, compute skew vs. a fixed server clock, and assert the monitor records
/// the correct source id, skew sign, and severity bucket.
/// </summary>
public sealed class ClockSkewMonitorTests
{
    [Fact]
    public void Probe_Records_Healthy_When_Skew_Is_Subsecond()
    {
        const string msh =
            "MSH|^~\\&|ACME Dialysis Machine^00059AFFFE3C7A00^EUI-64|FACILITY||" +
            "|20260522135959||ORU^R40^ORU_R40|MSG-1|P|2.6\r" +
            "PID|||MRN-1\r";
        var monitor = new InMemoryClockSkewMonitor();
        var serverNow = new DateTime(2026, 5, 22, 13, 59, 59, 500, DateTimeKind.Utc);

        var observed = Hl7V2ClockSkewProbe.TryObserve(Hl7V2Message.Parse(msh), serverNow, monitor);

        Assert.True(observed);
        var statuses = monitor.List();
        var status = Assert.Single(statuses);
        Assert.Equal("ACME Dialysis Machine@FACILITY", status.SourceId);
        Assert.True(Math.Abs(status.LastSkew.TotalSeconds) < 1.0);
        Assert.Equal("healthy", status.Severity);
        Assert.Equal(1, status.ObservationCount);
    }

    [Fact]
    public void Probe_Records_Warning_When_Skew_Is_Multiple_Seconds()
    {
        const string msh =
            "MSH|^~\\&|MachineA||||20260522135900||ORU^R40^ORU_R40|MSG-2|P|2.6\r";
        var monitor = new InMemoryClockSkewMonitor();
        // Server clock is 59s ahead of the message timestamp → skew = +00:00:59.
        var serverNow = new DateTime(2026, 5, 22, 13, 59, 59, DateTimeKind.Utc);

        Assert.True(Hl7V2ClockSkewProbe.TryObserve(Hl7V2Message.Parse(msh), serverNow, monitor));

        var status = Assert.Single(monitor.List());
        Assert.InRange(status.LastSkew.TotalSeconds, 58, 60);
        Assert.Equal("alert", status.Severity);
    }

    [Fact]
    public void Probe_Tracks_Max_Abs_Skew_Across_Multiple_Observations()
    {
        const string mshA =
            "MSH|^~\\&|MachineB||||20260522140000||ORU^R40^ORU_R40|MSG-3|P|2.6\r";
        const string mshB =
            "MSH|^~\\&|MachineB||||20260522135950||ORU^R40^ORU_R40|MSG-4|P|2.6\r";
        var monitor = new InMemoryClockSkewMonitor();
        var serverNow = new DateTime(2026, 5, 22, 14, 0, 0, DateTimeKind.Utc);

        Hl7V2ClockSkewProbe.TryObserve(Hl7V2Message.Parse(mshA), serverNow, monitor); // skew 0
        Hl7V2ClockSkewProbe.TryObserve(Hl7V2Message.Parse(mshB), serverNow, monitor); // skew +10s

        var status = Assert.Single(monitor.List());
        Assert.Equal(2, status.ObservationCount);
        Assert.InRange(status.MaxAbsSkewWindow.TotalSeconds, 9, 11);
    }

    [Fact]
    public void Probe_Returns_False_When_Msh_7_Is_Missing()
    {
        const string msh =
            "MSH|^~\\&|MachineC|||||ORU^R40^ORU_R40|MSG-5|P|2.6\r";
        var monitor = new InMemoryClockSkewMonitor();

        var observed = Hl7V2ClockSkewProbe.TryObserve(
            Hl7V2Message.Parse(msh), DateTime.UtcNow, monitor);

        Assert.False(observed);
        Assert.Empty(monitor.List());
    }

    [Theory]
    [InlineData("20260522135900", 2026, 5, 22, 13, 59, 0)]
    [InlineData("20260522135900.123", 2026, 5, 22, 13, 59, 0)]
    [InlineData("202605221359", 2026, 5, 22, 13, 59, 0)]
    [InlineData("20260522", 2026, 5, 22, 0, 0, 0)]
    public void Timestamp_Parser_Handles_Ig_Sample_Shapes(
        string raw, int y, int mo, int d, int h, int mi, int s)
    {
        Assert.True(Hl7V2ClockSkewProbe.TryParseHl7Timestamp(raw, out var utc));
        Assert.Equal(new DateTime(y, mo, d, h, mi, s, DateTimeKind.Utc), utc);
    }

    [Fact]
    public void Timestamp_Parser_Applies_Negative_Offset()
    {
        Assert.True(Hl7V2ClockSkewProbe.TryParseHl7Timestamp("20260522135900-0500", out var utc));
        // Local was 13:59 at -0500, so UTC = 18:59.
        Assert.Equal(new DateTime(2026, 5, 22, 18, 59, 0, DateTimeKind.Utc), utc);
    }
}
