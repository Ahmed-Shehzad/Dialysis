namespace Dialysis.Treatment.Application.Abstractions;

/// <summary>
/// Parses HL7 Batch Protocol (FHS/BHS/MSH.../BTS/FTS) and extracts individual messages.
/// </summary>
public interface IHl7BatchParser
{
    /// <summary>
    /// Extracts HL7 messages from a batch envelope.
    /// Strips FHS, BHS, BTS, FTS and returns each message that starts with MSH.
    /// </summary>
    IReadOnlyList<string> ExtractMessages(string batchMessage);
}
