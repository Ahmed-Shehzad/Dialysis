using System.Text;

namespace Dialysis.SmartConnect.DataTypes;

/// <summary>
/// Reusable streaming CSV / TSV / pipe-delimited-text reader, extracted from
/// <c>DelimitedTextTransformStage</c> (slice L2) so the File Reader (slice D2) can fan a
/// CSV out into one inbound <see cref="IntegrationMessage"/> per record without taking a
/// dependency on the transform-stage type or duplicating the parser. Pure pull-based: a
/// caller iterates <see cref="EnumerateRecords"/> and gets each row as a fresh
/// <c>string[]</c>; nothing accumulates between yields.
/// </summary>
public static class DelimitedTextStreaming
{
    /// <summary>Operator-facing options for the streaming reader.</summary>
    public sealed record Options
    {
        /// <summary>Operator-facing options for the streaming reader.</summary>
        public Options(char Delimiter = ',',
            bool HasHeaderRow = true,
            bool TrimWhitespace = true,
            bool SkipBlankLines = true)
        {
            this.Delimiter = Delimiter;
            this.HasHeaderRow = HasHeaderRow;
            this.TrimWhitespace = TrimWhitespace;
            this.SkipBlankLines = SkipBlankLines;
        }
        public static Options Default { get; } = new();
        public char Delimiter { get; init; }
        public bool HasHeaderRow { get; init; }
        public bool TrimWhitespace { get; init; }
        public bool SkipBlankLines { get; init; }
        public void Deconstruct(out char Delimiter, out bool HasHeaderRow, out bool TrimWhitespace, out bool SkipBlankLines)
        {
            Delimiter = this.Delimiter;
            HasHeaderRow = this.HasHeaderRow;
            TrimWhitespace = this.TrimWhitespace;
            SkipBlankLines = this.SkipBlankLines;
        }
    }

    /// <summary>
    /// Pulls the first non-blank row off the reader to serve as the header. Returns
    /// <c>null</c> when the stream is empty (so the projection / fan-out caller can emit
    /// an empty result gracefully).
    /// </summary>
    public static string[]? ReadHeader(StreamReader reader, Options options)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(options);
        while (reader.ReadLine() is { } rawLine)
        {
            if (options.SkipBlankLines && string.IsNullOrWhiteSpace(rawLine))
                continue;
            return MaterialiseRow(rawLine, options);
        }
        return null;
    }

    /// <summary>
    /// Lazy iterator. Reads lines until EOF, skipping blanks per options, and yields each
    /// parsed row. Each yielded array is freshly allocated; the caller is free to drop the
    /// reference once consumed.
    /// </summary>
    public static IEnumerable<string[]> EnumerateRecords(StreamReader reader, Options options)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(options);
        return EnumerateRecordsCore(reader, options);
    }

    private static IEnumerable<string[]> EnumerateRecordsCore(StreamReader reader, Options options)
    {
        while (reader.ReadLine() is { } rawLine)
        {
            if (options.SkipBlankLines && string.IsNullOrWhiteSpace(rawLine))
                continue;
            yield return MaterialiseRow(rawLine, options);
        }
    }

    /// <summary>Parse one logical line into a field array; applies trim per options.</summary>
    public static string[] MaterialiseRow(string rawLine, Options options)
    {
        ArgumentNullException.ThrowIfNull(rawLine);
        ArgumentNullException.ThrowIfNull(options);
        var fields = SplitRespectingQuotes(rawLine, options.Delimiter);
        if (options.TrimWhitespace)
        {
            for (var i = 0; i < fields.Length; i++)
                fields[i] = fields[i].Trim();
        }
        return fields;
    }

    /// <summary>
    /// Minimal RFC 4180 splitter: handles double-quoted fields with embedded delimiters and
    /// escaped quotes (<c>""</c>). Doesn't try to be a full CSV library — the dialysis CSV
    /// drops we've seen are well-formed; if a partner ships exotic CSV, swap this for
    /// CsvHelper in a follow-up. Multi-line quoted fields are NOT supported (we split on
    /// the line reader, which can't see across a <c>\n</c> inside a quoted cell).
    /// </summary>
    public static string[] SplitRespectingQuotes(string line, char delimiter)
    {
        ArgumentNullException.ThrowIfNull(line);
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    // Consume the second quote of the "" escape pair — the standard
                    // CSV lookahead-skip, so the index advance inside the body is deliberate.
#pragma warning disable S127
                    i++;
#pragma warning restore S127
                    continue;
                }
                if (c == '"')
                {
                    inQuotes = false;
                    continue;
                }
                current.Append(c);
                continue;
            }

            if (c == '"')
            {
                inQuotes = true;
                continue;
            }
            if (c == delimiter)
            {
                fields.Add(current.ToString());
                current.Clear();
                continue;
            }
            current.Append(c);
        }
        fields.Add(current.ToString());
        return [.. fields];
    }

    /// <summary>
    /// Accept the symbolic delimiter names operators are likely to type into a JSON config —
    /// <c>"\t"</c> / <c>"tab"</c> → <c>'\t'</c>, <c>"|"</c> / <c>"pipe"</c> → <c>'|'</c>,
    /// single-char strings as-is.
    /// </summary>
    public static char ResolveDelimiter(string raw) => raw switch
    {
        "\\t" or "tab" or "TAB" => '\t',
        "pipe" or "PIPE" => '|',
        _ when raw.Length == 1 => raw[0],
        _ => raw[0], // first character — defensive fallback so a stray multi-char value still parses
    };
}
