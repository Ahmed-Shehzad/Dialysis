using System.Text;

namespace Dialysis.PDMS.Medications.IvPumps;

/// <summary>
/// Standards-conformant HL7 v2 PCD-04 (Point-of-Care Device data, IHE PCD-04 profile) parser.
/// Any vendor that implements the IHE PCD-04 message structure feeds into this driver
/// regardless of brand. The implementation here parses the minimal subset relevant for
/// infusion telemetry: OBR + OBX rows for programmed rate, actual rate, programmed volume,
/// infused volume. ROL / ORC / specimen fields are ignored — the chairside use-case only
/// needs the observation values.
///
/// Recognised OBX codes (LOINC):
/// <list type="bullet">
///   <item>69869-3 — IV infusion rate (mL/h)</item>
///   <item>69870-1 — IV infused volume (mL)</item>
///   <item>69871-9 — IV programmed rate (mL/h)</item>
///   <item>69872-7 — IV programmed volume (mL)</item>
/// </list>
/// </summary>
public sealed class Pcd04NormalisedDriver : IIvPumpDriver
{
    public string VendorCode => "pcd04";

    public Task<IvPumpReading> ParseAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        var text = Encoding.UTF8.GetString(payload.Span);
        var segments = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        string? deviceId = null;
        DateTime timestamp = DateTime.UtcNow;
        long sequence = 0;
        decimal? programmedRate = null;
        decimal? actualRate = null;
        decimal? programmedVolume = null;
        decimal? infusedVolume = null;
        string? rxnormCode = null;

        foreach (var segment in segments)
        {
            var fields = segment.Split('|');
            if (fields.Length == 0) continue;

            switch (fields[0])
            {
                case "MSH":
                    // MSH-7 = message timestamp; MSH-10 = control id (use as sequence proxy).
                    if (fields.Length > 7 && DateTime.TryParseExact(fields[6], "yyyyMMddHHmmss", null,
                        System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                        out var ts))
                    {
                        timestamp = ts;
                    }
                    if (fields.Length > 10 && long.TryParse(fields[9], out var seq))
                    {
                        sequence = seq;
                    }
                    break;

                case "PRT":
                    // PRT-10 carries the device id in PCD-04 (Participation segment).
                    // fields[0]=segment name, fields[N]=PRT-N → device id at index 10.
                    if (fields.Length > 10) deviceId = fields[10];
                    break;

                case "OBX":
                    // OBX-3 = observation identifier (LOINC); OBX-5 = value. fields[0]=segment name.
                    if (fields.Length < 6) break;
                    var idParts = fields[3].Split('^');
                    var code = idParts.Length > 0 ? idParts[0] : string.Empty;
                    if (!decimal.TryParse(fields[5], System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture, out var val))
                    {
                        break;
                    }
                    switch (code)
                    {
                        case "69869-3": actualRate = val; break;
                        case "69870-1": infusedVolume = val; break;
                        case "69871-9": programmedRate = val; break;
                        case "69872-7": programmedVolume = val; break;
                    }
                    break;

                case "RXR":
                    // RXR-1 in PCD-04 carries the drug RxNorm code in many vendor profiles.
                    if (fields.Length > 1)
                    {
                        var coding = fields[1].Split('^');
                        if (coding.Length > 0 && !string.IsNullOrEmpty(coding[0]))
                        {
                            rxnormCode = coding[0];
                        }
                    }
                    break;
            }
        }

        if (string.IsNullOrEmpty(deviceId))
        {
            throw new FormatException("PCD-04 payload missing PRT-10 device identifier.");
        }

        // Infer the reading kind from which fields are present.
        var kind = (programmedRate.HasValue, infusedVolume, programmedVolume) switch
        {
            (true, _, _) when programmedVolume.HasValue && (infusedVolume ?? 0) == 0 => IvPumpReadingKind.Start,
            (_, decimal iv, decimal pv) when iv >= pv => IvPumpReadingKind.Complete,
            _ => IvPumpReadingKind.Progress,
        };

        return Task.FromResult(new IvPumpReading(
            VendorCode: VendorCode,
            PumpDeviceId: deviceId!,
            SequenceNumber: sequence,
            Kind: kind,
            CapturedAtUtc: timestamp,
            ProgrammedRateMlPerHour: programmedRate,
            ActualRateMlPerHour: actualRate,
            ProgrammedVolumeMl: programmedVolume,
            InfusedVolumeMl: infusedVolume,
            MedicationCodeSystem: rxnormCode is null ? null : "http://www.nlm.nih.gov/research/umls/rxnorm",
            MedicationCode: rxnormCode,
            AlarmCode: null,
            AlarmText: null,
            AlarmSeverity: null));
    }
}
