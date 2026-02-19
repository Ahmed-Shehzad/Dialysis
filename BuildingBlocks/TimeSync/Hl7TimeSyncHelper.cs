using System.Globalization;

namespace BuildingBlocks.TimeSync;

/// <summary>
/// Extracts MSH-7 (message datetime) from HL7 messages and computes drift from server UTC.
/// Used for IHE Consistent Time alignment validation.
/// </summary>
public static class Hl7TimeSyncHelper
{
    private static readonly string[] Hl7DateTimeFormats =
    [
        "yyyyMMddHHmmss",
        "yyyyMMddHHmmsszzz",
        "yyyyMMddHHmmss.ffffff",
        "yyyyMMddHHmmss.ffffffzzz"
    ];

    /// <summary>
    /// Extracts MSH-7 (DateTime of Message) from raw HL7. Returns null if not found or unparseable.
    /// </summary>
    public static DateTimeOffset? ExtractMessageTimestamp(string rawHl7Message)
    {
        if (string.IsNullOrWhiteSpace(rawHl7Message)) return null;

        string firstLine = rawHl7Message.TrimStart();
        int lineEnd = firstLine.IndexOfAny(['\r', '\n']);
        if (lineEnd >= 0) firstLine = firstLine[..lineEnd];

        if (!firstLine.StartsWith("MSH|", StringComparison.Ordinal)) return null;

        string[] fields = firstLine.Split('|');
        // MSH: [0]=MSH, [1]=^~\&, [2-5]=apps/facilities, [6]=MSH-7 datetime
        string? value = fields.Length > 6 ? fields[6].Trim() : null;
        return ParseHl7DateTime(value);
    }

    /// <summary>
    /// Returns absolute drift in seconds between message time and server UTC. Null if message time is null.
    /// </summary>
    public static double? GetDriftSeconds(DateTimeOffset? messageTime) => !messageTime.HasValue ? null : Math.Abs((messageTime.Value - DateTimeOffset.UtcNow).TotalSeconds);

    private static DateTimeOffset? ParseHl7DateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        foreach (string format in Hl7DateTimeFormats)
            if (DateTimeOffset.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset result))
                return result;

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset parsed) ? parsed : null;
    }
}
