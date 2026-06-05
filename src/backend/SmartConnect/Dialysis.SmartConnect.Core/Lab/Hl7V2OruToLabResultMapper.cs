using System.Globalization;
using Dialysis.SmartConnect.DataTypes;

namespace Dialysis.SmartConnect.Lab;

/// <summary>
/// Parses an inbound HL7 v2 <c>ORU^R01</c> message into a transport-neutral
/// <see cref="LabResultFrame"/> — the inbound counterpart of <see cref="Hl7V2OrmO01Builder"/>.
/// Reads the placer/filler order numbers from ORC/OBR, the patient identifier from PID-3, the
/// result timestamp from OBR-7, and one <see cref="LabResultObservation"/> per OBX segment
/// (OBX-3 LOINC, OBX-5 value, OBX-6 units, OBX-7 reference range, OBX-8 abnormal flag, OBX-11
/// result status). Returns <see langword="null"/> when the message has no placer order number or
/// no observations, so a caller can pass an unexpected payload through.
/// </summary>
public static class Hl7V2OruToLabResultMapper
{
    /// <summary>Projects an ORU^R01 message onto a <see cref="LabResultFrame"/>; null when not a usable result.</summary>
    public static LabResultFrame? TryMap(Hl7V2Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var orc = FirstSegment(message, "ORC");
        var obr = FirstSegment(message, "OBR");

        // Placer order number: ORC-2 then OBR-2; filler: ORC-3 then OBR-3.
        var placer = Component(orc, 2, 1) ?? Component(obr, 2, 1);
        if (string.IsNullOrWhiteSpace(placer))
        {
            return null;
        }

        var filler = Component(orc, 3, 1) ?? Component(obr, 3, 1);
        var patient = Component(FirstSegment(message, "PID"), 3, 1) ?? string.Empty;
        var resultedAt = ParseTs(Component(obr, 7, 0)) ?? DateTime.UtcNow;

        var observations = new List<LabResultObservation>();
        var anyFinal = false;
        foreach (var obx in message.Segments.Where(s => string.Equals(s.Name, "OBX", StringComparison.OrdinalIgnoreCase)))
        {
            var code = Component(obx, 3, 1);
            var value = Component(obx, 5, 0);
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            observations.Add(new LabResultObservation(
                Code: code,
                Display: Component(obx, 3, 2) ?? code,
                Value: value,
                Unit: NullIfEmpty(Component(obx, 6, 1)),
                ReferenceRange: NullIfEmpty(Component(obx, 7, 0)),
                Interpretation: NullIfEmpty(Component(obx, 8, 1))));

            if (string.Equals(Component(obx, 11, 1), "F", StringComparison.OrdinalIgnoreCase))
            {
                anyFinal = true;
            }
        }

        if (observations.Count == 0)
        {
            return null;
        }

        return new LabResultFrame(
            PatientIdentifier: patient,
            PlacerOrderNumber: placer,
            FillerOrderNumber: NullIfEmpty(filler),
            IsFinal: anyFinal,
            Observations: observations,
            ResultedAtUtc: resultedAt);
    }

    private static Hl7Segment? FirstSegment(Hl7V2Message message, string name) =>
        message.Segments.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Reads a 1-based field/component from a segment's first repeat; component 0 joins all components.</summary>
    private static string? Component(Hl7Segment? segment, int field1Based, int component1Based)
    {
        if (segment is null)
        {
            return null;
        }

        var fieldIndex = field1Based - 1;
        if (fieldIndex < 0 || fieldIndex >= segment.Fields.Count)
        {
            return null;
        }

        var repeats = segment.Fields[fieldIndex];
        if (repeats.Length == 0)
        {
            return null;
        }

        var components = repeats[0];
        if (component1Based == 0)
        {
            return components.Length == 0 ? null : string.Join('^', components);
        }

        var ci = component1Based - 1;
        return ci >= 0 && ci < components.Length ? components[ci] : null;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static DateTime? ParseTs(string? ts)
    {
        if (string.IsNullOrWhiteSpace(ts))
        {
            return null;
        }

        // HL7 TS: yyyyMMdd[HHmmss][+ZZZZ]; take the leading numeric run, parse what we can.
        var digits = new string(ts.TakeWhile(char.IsDigit).ToArray());
        var formats = new[] { "yyyyMMddHHmmss", "yyyyMMddHHmm", "yyyyMMddHH", "yyyyMMdd" };
        foreach (var format in formats)
        {
            if (digits.Length >= format.Length
                && DateTime.TryParseExact(
                    digits[..format.Length],
                    format,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }
}
