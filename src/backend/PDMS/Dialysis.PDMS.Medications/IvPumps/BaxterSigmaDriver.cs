using System.Text.Json;

namespace Dialysis.PDMS.Medications.IvPumps;

/// <summary>
/// Baxter SIGMA Spectrum drug-library JSON parser. SIGMA exposes a slightly different field
/// naming convention than BD Alaris: drugCode (ATC) instead of rxnorm, mlPerHr instead of
/// mlH, eventType instead of event. Logically equivalent but field-by-field different.
///
/// Example payload:
/// <code>
/// {
///   "deviceId": "SIGMA-CH4-PUMP-3",
///   "eventType": "INFUSION_PROGRESS",
///   "seq": 142,
///   "ts": "2026-06-01T12:34:56Z",
///   "rate": { "programmedMlPerHr": 100.0, "actualMlPerHr": 99.5 },
///   "volume": { "programmedMl": 250.0, "infusedMl": 142.0 },
///   "drug": { "atc": "B01AB01", "name": "Heparin" }
/// }
/// </code>
/// </summary>
public sealed class BaxterSigmaDriver : IIvPumpDriver
{
    public string VendorCode => "baxter-sigma";

    public Task<IvPumpReading> ParseAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        var deviceId = root.GetProperty("deviceId").GetString()
            ?? throw new FormatException("Baxter SIGMA payload missing deviceId.");
        var eventType = root.GetProperty("eventType").GetString() ?? "INFUSION_PROGRESS";
        var sequence = root.TryGetProperty("seq", out var s) ? s.GetInt64() : 0L;
        var timestamp = root.TryGetProperty("ts", out var t) && t.ValueKind == JsonValueKind.String
            ? DateTime.Parse(t.GetString()!, null, System.Globalization.DateTimeStyles.RoundtripKind)
            : DateTime.UtcNow;

        decimal? programmedRate = null;
        decimal? actualRate = null;
        if (root.TryGetProperty("rate", out var rate) && rate.ValueKind == JsonValueKind.Object)
        {
            programmedRate = rate.TryGetProperty("programmedMlPerHr", out var pr) ? pr.GetDecimal() : null;
            actualRate = rate.TryGetProperty("actualMlPerHr", out var ar) ? ar.GetDecimal() : null;
        }

        decimal? programmedVolume = null;
        decimal? infusedVolume = null;
        if (root.TryGetProperty("volume", out var vol) && vol.ValueKind == JsonValueKind.Object)
        {
            programmedVolume = vol.TryGetProperty("programmedMl", out var pv) ? pv.GetDecimal() : null;
            infusedVolume = vol.TryGetProperty("infusedMl", out var iv) ? iv.GetDecimal() : null;
        }

        string? atcCode = null;
        string? atcSystem = null;
        if (root.TryGetProperty("drug", out var drug) && drug.ValueKind == JsonValueKind.Object &&
            drug.TryGetProperty("atc", out var atc))
        {
            atcCode = atc.GetString();
            if (!string.IsNullOrEmpty(atcCode))
            {
                atcSystem = "http://www.whocc.no/atc";
            }
        }

        var kind = eventType switch
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
            MedicationCodeSystem: atcSystem,
            MedicationCode: atcCode,
            AlarmCode: null,
            AlarmText: null,
            AlarmSeverity: null));
    }
}
