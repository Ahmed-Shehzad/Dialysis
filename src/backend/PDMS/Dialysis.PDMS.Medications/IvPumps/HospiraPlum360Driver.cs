using System.Globalization;
using System.Text.Json;

namespace Dialysis.PDMS.Medications.IvPumps;

/// <summary>
/// Hospira / ICU Medical Plum 360 parser. Plum 360 exposes a flatter JSON envelope per ICU
/// Medical's MedNet adapter spec. Field names lean toward snake_case; sequence is implicit
/// in the per-device monotonic ts.
///
/// Example payload:
/// <code>
/// {
///   "pump_id": "PLUM360-CH5-PUMP-2",
///   "kind": "progress",
///   "ts": "2026-06-01T12:34:56Z",
///   "programmed_rate_ml_h": 100.0,
///   "actual_rate_ml_h": 99.7,
///   "programmed_volume_ml": 250.0,
///   "infused_volume_ml": 142.0,
///   "drug_rxnorm": "1234"
/// }
/// </code>
/// </summary>
public sealed class HospiraPlum360Driver : IIvPumpDriver
{
    public string VendorCode => "plum-360";

    public Task<IvPumpReading> ParseAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        var deviceId = root.GetProperty("pump_id").GetString()
            ?? throw new FormatException("Plum 360 payload missing pump_id.");
        var kindStr = root.GetProperty("kind").GetString() ?? "progress";
        var timestamp = root.TryGetProperty("ts", out var t) && t.ValueKind == JsonValueKind.String
            ? DateTime.Parse(t.GetString()!, null, DateTimeStyles.RoundtripKind)
            : DateTime.UtcNow;
        // Plum 360 doesn't include an explicit sequence; use the Unix ticks of the timestamp
        // as a monotonic proxy so the dispatch path can correlate updates.
        var sequence = new DateTimeOffset(timestamp, TimeSpan.Zero).ToUnixTimeMilliseconds();

        var programmedRate = ReadDecimal(root, "programmed_rate_ml_h");
        var actualRate = ReadDecimal(root, "actual_rate_ml_h");
        var programmedVolume = ReadDecimal(root, "programmed_volume_ml");
        var infusedVolume = ReadDecimal(root, "infused_volume_ml");

        string? rxnormCode = root.TryGetProperty("drug_rxnorm", out var rx) ? rx.GetString() : null;
        string? rxnormSystem = string.IsNullOrEmpty(rxnormCode)
            ? null
            : "http://www.nlm.nih.gov/research/umls/rxnorm";

        var kind = kindStr.ToLowerInvariant() switch
        {
            "start" => IvPumpReadingKind.Start,
            "progress" => IvPumpReadingKind.Progress,
            "pause" => IvPumpReadingKind.Pause,
            "resume" => IvPumpReadingKind.Resume,
            "complete" => IvPumpReadingKind.Complete,
            "alarm" => IvPumpReadingKind.Alarm,
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
            AlarmCode: null,
            AlarmText: null,
            AlarmSeverity: null));
    }

    private static decimal? ReadDecimal(JsonElement parent, string property)
    {
        if (!parent.TryGetProperty(property, out var prop)) return null;
        return prop.ValueKind == JsonValueKind.Number ? prop.GetDecimal() : null;
    }
}
