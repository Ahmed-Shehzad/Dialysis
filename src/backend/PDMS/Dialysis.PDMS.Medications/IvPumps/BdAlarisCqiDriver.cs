using System.Text.Json;
using Dialysis.PDMS.Medications.Contracts;

namespace Dialysis.PDMS.Medications.IvPumps;

/// <summary>
/// BD Alaris CareFusion Connectivity Interface (CQI) parser. CQI is a JSON-over-HTTP
/// adapter pushed by the BD Alaris gateway; the on-wire shape is described in BD's
/// integration specification. We parse the subset required to drive the
/// <see cref="IvPumpReading"/> shape; other fields are silently ignored.
///
/// Example payload:
/// <code>
/// {
///   "device": { "id": "ALARIS-CH4-PUMP-7" },
///   "event": "INFUSION_PROGRESS",
///   "sequence": 1421,
///   "timestamp": "2026-06-01T12:34:56Z",
///   "infusion": {
///     "programmedRateMlH": 100.0,
///     "actualRateMlH": 99.8,
///     "programmedVolumeMl": 250.0,
///     "infusedVolumeMl": 142.0,
///     "drug": { "rxnorm": "1234", "name": "Heparin" }
///   }
/// }
/// </code>
/// </summary>
public sealed class BdAlarisCqiDriver : IIvPumpDriver
{
    public string VendorCode => "bd-alaris";

    public Task<IvPumpReading> ParseAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        var deviceId = root.GetProperty("device").GetProperty("id").GetString()
            ?? throw new FormatException("BD Alaris CQI payload missing device.id.");
        var eventCode = root.GetProperty("event").GetString() ?? "INFUSION_PROGRESS";
        var sequence = root.TryGetProperty("sequence", out var s) ? s.GetInt64() : 0L;
        var timestamp = root.TryGetProperty("timestamp", out var t) && t.ValueKind == JsonValueKind.String
            ? DateTime.Parse(t.GetString()!, null, System.Globalization.DateTimeStyles.RoundtripKind)
            : DateTime.UtcNow;

        var infusion = root.TryGetProperty("infusion", out var inf) ? inf : default;
        decimal? programmedRate = ReadDecimal(infusion, "programmedRateMlH");
        decimal? actualRate = ReadDecimal(infusion, "actualRateMlH");
        decimal? programmedVolume = ReadDecimal(infusion, "programmedVolumeMl");
        decimal? infusedVolume = ReadDecimal(infusion, "infusedVolumeMl");

        string? rxnormCode = null;
        string? rxnormSystem = null;
        if (infusion.ValueKind == JsonValueKind.Object &&
            infusion.TryGetProperty("drug", out var drug) &&
            drug.TryGetProperty("rxnorm", out var rx))
        {
            rxnormCode = rx.GetString();
            if (!string.IsNullOrEmpty(rxnormCode))
            {
                rxnormSystem = "http://www.nlm.nih.gov/research/umls/rxnorm";
            }
        }

        string? alarmCode = null;
        string? alarmText = null;
        IvPumpAlarmSeverity? alarmSeverity = null;
        if (root.TryGetProperty("alarm", out var alarm) && alarm.ValueKind == JsonValueKind.Object)
        {
            alarmCode = alarm.TryGetProperty("code", out var ac) ? ac.GetString() : null;
            alarmText = alarm.TryGetProperty("text", out var at) ? at.GetString() : null;
            alarmSeverity = alarm.TryGetProperty("severity", out var sev) && sev.ValueKind == JsonValueKind.String
                ? sev.GetString() switch
                {
                    "CRITICAL" or "critical" => IvPumpAlarmSeverity.Critical,
                    "WARNING" or "warning" => IvPumpAlarmSeverity.Warning,
                    _ => IvPumpAlarmSeverity.Informational,
                }
                : null;
        }

        var kind = eventCode switch
        {
            "INFUSION_START" => IvPumpReadingKind.Start,
            "INFUSION_PROGRESS" => IvPumpReadingKind.Progress,
            "INFUSION_PAUSE" => IvPumpReadingKind.Pause,
            "INFUSION_RESUME" => IvPumpReadingKind.Resume,
            "INFUSION_COMPLETE" => IvPumpReadingKind.Complete,
            "ALARM" => IvPumpReadingKind.Alarm,
            _ => IvPumpReadingKind.Progress,
        };

        return Task.FromResult(new IvPumpReading(
            VendorCode: VendorCode,
            PumpDeviceId: deviceId,
            SequenceNumber: sequence,
            Kind: kind,
            CapturedAtUtc: timestamp,
            ProgrammedRateMlPerHour: programmedRate,
            ActualRateMlPerHour: actualRate,
            ProgrammedVolumeMl: programmedVolume,
            InfusedVolumeMl: infusedVolume,
            MedicationCodeSystem: rxnormSystem,
            MedicationCode: rxnormCode,
            AlarmCode: alarmCode,
            AlarmText: alarmText,
            AlarmSeverity: alarmSeverity));
    }

    private static decimal? ReadDecimal(JsonElement parent, string property)
    {
        if (parent.ValueKind != JsonValueKind.Object) return null;
        if (!parent.TryGetProperty(property, out var prop)) return null;
        return prop.ValueKind switch
        {
            JsonValueKind.Number => prop.GetDecimal(),
            JsonValueKind.String when decimal.TryParse(prop.GetString(), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var d) => d,
            _ => null,
        };
    }
}
