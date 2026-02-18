namespace Dialysis.Alarm.Application.Abstractions;

/// <summary>
/// Parses HL7 ORU^R40 (PCD-04) alarm messages.
/// </summary>
public interface IOruR40MessageParser
{
    OruR40ParseResult Parse(string hl7Message);
}

/// <summary>
/// Result of parsing an ORU^R40 message.
/// </summary>
public sealed record OruR40ParseResult(
    string? DeviceId,
    string? SessionId,
    IReadOnlyList<AlarmInfo> Alarms);
