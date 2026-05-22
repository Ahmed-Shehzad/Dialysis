using System.Globalization;
using System.Text;

namespace Dialysis.SmartConnect.TreatmentReport;

/// <summary>
/// Builds the IG §6.2 PCD-01 treatment-report wire frame: <c>ORU^R40^ORU_R40</c> with the
/// minimal segment set (MSH, PID, OBR, OBX*) that the rev 4 IG mandates. The IG samples in
/// §6.2.4 / §6.2.5 use the legacy <c>ORU^R01</c> trigger; this builder emits <c>ORU^R40</c>
/// per the rev 4 update (§6 header text), matching slice 3's mapper alias.
/// </summary>
/// <remarks>
/// OBX-17 (Observation Method) is emitted via the slice-5
/// <see cref="ObservationSourceExtensions.ToObx17Cwe"/> mapping when the
/// <see cref="ObservationFrame.Source"/> is set:
/// <list type="bullet">
///   <item>OBX-17 is REQUIRED for settings (RSET / MSET / ASET) per IG §8.2.5 line 1474.</item>
///   <item>OBX-17 is OPTIONAL for measurements (AMEAS / MMEAS); the builder emits it
///         when present so an EMR can distinguish operator-triggered NIBP readings
///         from continuous sensor streams.</item>
///   <item>Container OBX rows (no value, no source — pure containment hierarchy) leave
///         OBX-17 empty.</item>
/// </list>
/// </remarks>
public static class Hl7V2OruR40Builder
{
    private const string MessageType = "ORU^R40^ORU_R40";
    private const string ProcessingId = "P";
    private const string VersionId = "2.6";

    public static string Build(
        TreatmentReportFrame report,
        string messageControlId,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageControlId);

        var sb = new StringBuilder();
        AppendMsh(sb, report.Machine, messageControlId, nowUtc);
        AppendPid(sb, report.PatientIdentifier);
        AppendObr(sb, report.Machine, report.ObservedAtUtc);
        var idx = 1;
        foreach (var obs in report.Observations)
            AppendObx(sb, ref idx, obs, report.ObservedAtUtc);
        return sb.ToString();
    }

    private static void AppendMsh(StringBuilder sb, MachineIdentity machine, string controlId, DateTime nowUtc)
    {
        sb.Append("MSH|^~\\&|")
          .Append(Sanitize(machine.ApplicationName)).Append('^')
          .Append(Sanitize(machine.DeviceIdentifier)).Append('^')
          .Append(Sanitize(machine.IdentifierAssigningAuthority))
          .Append("||||").Append(FormatTs(nowUtc))
          .Append("||").Append(MessageType).Append('|').Append(controlId).Append('|')
          .Append(ProcessingId).Append('|').Append(VersionId)
          .Append("|||AL|NE|||||\r");
    }

    private static void AppendPid(StringBuilder sb, string patientIdentifier) =>
        sb.Append("PID|||").Append(Sanitize(patientIdentifier)).Append("^^^^MR\r");

    private static void AppendObr(StringBuilder sb, MachineIdentity machine, DateTime observedAtUtc)
    {
        // OBR-3 placer order number: <ts>^<app>^<device>^<authority>, ts = observed timestamp.
        // OBR-4 universal service identifier: MachineMds (top of containment tree).
        sb.Append("OBR|1||")
          .Append(FormatTs(observedAtUtc)).Append('^')
          .Append(Sanitize(machine.ApplicationName)).Append('^')
          .Append(Sanitize(machine.DeviceIdentifier)).Append('^')
          .Append(Sanitize(machine.IdentifierAssigningAuthority))
          .Append("|70929^MDC_DEV_HDIALY_MACHINE_MDS^MDC|||")
          .Append(FormatTs(observedAtUtc)).Append('\r');
    }

    private static void AppendObx(StringBuilder sb, ref int idx, ObservationFrame obs, DateTime observedAtUtc)
    {
        // Wire layout: OBX|setId|type|obsId|containment|value|units|||||F||obsTime||obx17
        // OBX-12 = observation result status (always F), OBX-13 = effective date (blank),
        // OBX-14 = observation date/time, OBX-15/16 blank, OBX-17 = observation method (source).
        sb.Append("OBX|").Append(idx).Append('|').Append(obs.ValueType).Append('|')
          .Append(obs.ObservationId).Append('|').Append(obs.ContainmentPath).Append('|')
          .Append(obs.Value).Append('|').Append(obs.Units ?? string.Empty)
          .Append("|||||F||||")
          .Append(FormatTs(observedAtUtc))
          .Append("||");
        if (obs.Source.HasValue)
            sb.Append(obs.Source.Value.ToObx17Cwe());
        sb.Append('\r');
        idx += 1;
    }

    private static string FormatTs(DateTime utc) =>
        utc.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + "+0000";

    private static string Sanitize(string value) =>
        value
            .Replace("|", string.Empty, StringComparison.Ordinal)
            .Replace("^", string.Empty, StringComparison.Ordinal)
            .Replace("~", string.Empty, StringComparison.Ordinal)
            .Replace("\\", string.Empty, StringComparison.Ordinal)
            .Replace("&", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
}
