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
            if (seg.StartsWith("FHS", StringComparison.Ordinal) ||
                seg.StartsWith("BHS", StringComparison.Ordinal))
                continue;

            if (seg.StartsWith("BTS", StringComparison.Ordinal) ||
                seg.StartsWith("FTS", StringComparison.Ordinal))
            {
                if (current is { Count: > 0 })
                {
                    messages.Add(string.Join('\r', current));
                    current = null;
                }
                continue;
            }

            if (seg.StartsWith("MSH", StringComparison.Ordinal))
            {
                if (current is { Count: > 0 })
                    messages.Add(string.Join('\r', current));

                current = [seg];
                continue;
            }

            if (current is not null)
                current.Add(seg);
        }

        if (current is { Count: > 0 })
            messages.Add(string.Join('\r', current));

        return messages;
    }
}
