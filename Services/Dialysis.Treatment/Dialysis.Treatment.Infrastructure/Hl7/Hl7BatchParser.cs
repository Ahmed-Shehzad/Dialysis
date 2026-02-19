namespace Dialysis.Treatment.Infrastructure.Hl7;

/// <summary>
/// Parses HL7 Batch Protocol (FHS/BHS/MSH.../BTS/FTS).
/// Extracts individual messages for processing (run sheet capture).
/// </summary>
public sealed class Hl7BatchParser : Application.Abstractions.IHl7BatchParser
{
    private const char SegmentTerminator = '\r';

    public IReadOnlyList<string> ExtractMessages(string batchMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchMessage);

        string normalized = batchMessage.Replace("\r\n", "\r").Replace("\n", "\r");
        string[] segments = normalized.Split(SegmentTerminator, StringSplitOptions.RemoveEmptyEntries);

        var messages = new List<string>();
        List<string>? current = null;

        foreach (string seg in segments)
        {
            if (IsBatchHeader(seg))
                continue;

            if (IsBatchTrailer(seg))
            {
                FlushMessage(current, messages);
                current = null;
                continue;
            }

            if (seg.StartsWith("MSH", StringComparison.Ordinal))
            {
                FlushMessage(current, messages);
                current = [seg];
                continue;
            }

            current?.Add(seg);
        }

        FlushMessage(current, messages);
        return messages;
    }

    private static bool IsBatchHeader(string seg) =>
        seg.StartsWith("FHS", StringComparison.Ordinal) || seg.StartsWith("BHS", StringComparison.Ordinal);

    private static bool IsBatchTrailer(string seg) =>
        seg.StartsWith("BTS", StringComparison.Ordinal) || seg.StartsWith("FTS", StringComparison.Ordinal);

    private static void FlushMessage(List<string>? current, List<string> messages)
    {
        if (current is { Count: > 0 })
            messages.Add(string.Join('\r', current));
    }
}
