using System.Globalization;

using BuildingBlocks.ValueObjects;

using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Domain.ValueObjects;

using Efferent.HL7.V2;

namespace Dialysis.Treatment.Infrastructure.Hl7;

/// <summary>
/// Parses HL7 ORU^R01 (PCD-01) messages into treatment observations.
/// </summary>
public sealed class OruR01Parser : IOruMessageParser
{
    /// <summary>
    /// Parse an ORU^R01 message and extract session metadata plus OBX observations.
    /// </summary>
    /// <param name="hl7Message">Raw HL7 message (pipe-delimited).</param>
    /// <returns>Parsed result with SessionId, PatientMrn, DeviceId, and Observations.</returns>
    public OruParseResult Parse(string hl7Message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hl7Message);

        var msg = new Message(hl7Message);
        if (!msg.ParseMessage(bypassValidation: true))
            throw new ArgumentException("Invalid HL7 ORU message.", nameof(hl7Message));

        var sessionId = GetSessionId(msg);
        var patientMrn = SafeGetValue(msg, "PID", 3);
        var deviceId = GetDeviceId(msg);

        var observations = new List<ObservationInfo>();
        var obxIndex = 1;
        while (true)
        {
            var code = SafeGetValue(msg, "OBX", obxIndex, 3);
            if (string.IsNullOrEmpty(code))
                break;

            var value = SafeGetValue(msg, "OBX", obxIndex, 5);
            var unit = SafeGetValue(msg, "OBX", obxIndex, 6);
            var subId = SafeGetValue(msg, "OBX", obxIndex, 4);
            var provenance = SafeGetValue(msg, "OBX", obxIndex, 17);
            var effectiveTimeStr = SafeGetValue(msg, "OBX", obxIndex, 14);
            DateTimeOffset? effectiveTime = null;
            if (DateTimeOffset.TryParse(effectiveTimeStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
                effectiveTime = dt;

            observations.Add(new ObservationInfo(new ObservationCode(code), value, unit, subId, provenance, effectiveTime));
            obxIndex++;
        }

        return new OruParseResult(
            new SessionId(sessionId),
            !string.IsNullOrWhiteSpace(patientMrn) ? new MedicalRecordNumber(patientMrn) : null,
            !string.IsNullOrWhiteSpace(deviceId) ? new DeviceId(deviceId) : null,
            observations);
    }

    private static string GetSessionId(Message msg)
    {
        var obr3 = SafeGetValue(msg, "OBR", 1, 3);
        if (!string.IsNullOrEmpty(obr3))
        {
            var parts = obr3.Split('^');
            if (parts.Length > 0 && !string.IsNullOrEmpty(parts[0]))
                return parts[0];
        }

        var msh10 = SafeGetValue(msg, "MSH", 10);
        if (!string.IsNullOrEmpty(msh10))
            return msh10;

        return Guid.NewGuid().ToString("N")[..12];
    }

    private static string? GetDeviceId(Message msg)
    {
        var msh3 = SafeGetValue(msg, "MSH", 3);
        if (!string.IsNullOrEmpty(msh3))
            return msh3;

        var obr3 = SafeGetValue(msg, "OBR", 1, 3);
        if (!string.IsNullOrEmpty(obr3))
        {
            var parts = obr3.Split('^');
            if (parts.Length >= 3)
                return parts[2];
            if (parts.Length >= 2)
                return parts[1];
        }

        return null;
    }

    private static string SafeGetValue(Message msg, string segment, int fieldIndex)
    {
        try
        {
            return msg.GetValue($"{segment}.{fieldIndex}") ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string SafeGetValue(Message msg, string segment, int occurrence, int fieldIndex)
    {
        try
        {
            return msg.GetValue($"{segment}({occurrence}).{fieldIndex}") ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
