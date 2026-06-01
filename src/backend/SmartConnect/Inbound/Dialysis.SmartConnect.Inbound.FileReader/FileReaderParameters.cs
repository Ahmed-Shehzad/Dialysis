namespace Dialysis.SmartConnect.Inbound.FileReader;

/// <summary>What to do with a file after it has been successfully dispatched.</summary>
public enum FileReaderAfterReadAction
{
    /// <summary>Permanently delete the file.</summary>
    Delete = 0,

    /// <summary>Move the file into <see cref="FileReaderParameters.MoveToDirectory"/>.</summary>
    MoveTo = 1,

    /// <summary>Leave the file in place. Caller is responsible for ensuring it is not re-read (file already handled).</summary>
    Leave = 2,
}

/// <summary>Slice D2: when the file contains multiple records, how the File Reader splits
/// it into per-record <see cref="IntegrationMessage"/>s. <see cref="None"/> preserves the
/// historical "whole file is one message" behaviour.</summary>
public enum FileReaderSplitMode
{
    /// <summary>One message per file (default; backward compatible).</summary>
    None = 0,

    /// <summary>Split at each <c>MSH|</c> segment so each HL7v2 message in a batched file
    /// becomes its own <see cref="IntegrationMessage"/>.</summary>
    Hl7v2 = 1,

    /// <summary>Split on newlines (<c>\r\n</c> / <c>\n</c> / <c>\r</c>); blank lines are
    /// skipped. Useful for line-delimited JSON / CSV-row drops.</summary>
    Line = 2,

    /// <summary>Custom regex split (<see cref="FileReaderParameters.SplitPattern"/> required).
    /// The pattern is treated as the boundary between records — capture groups are
    /// preserved in the records, matching split semantics.</summary>
    Regex = 3,

    /// <summary>Delimited-text records (CSV / TSV / pipe). Each non-blank row in the file
    /// becomes its own <see cref="IntegrationMessage"/> via the streaming reader shared
    /// with <c>DelimitedTextTransformStage</c> (slice L2). Reuses
    /// <see cref="FileReaderParameters.DelimitedTextDelimiter"/> /
    /// <see cref="FileReaderParameters.DelimitedTextHasHeaderRow"/> for the parser options;
    /// the header row (when configured) is dropped from the dispatched records.</summary>
    DelimitedTextRecords = 4,
}

/// <summary>
/// Strongly-typed parsed view of <see cref="SourceConnectorContext.Parameters"/> for the file reader.
/// </summary>
public sealed class FileReaderParameters
{
    public required string Directory { get; init; }

    public string FilePattern { get; init; } = "*";

    public int PollIntervalSeconds { get; init; } = 5;

    public FileReaderAfterReadAction AfterRead { get; init; } = FileReaderAfterReadAction.Delete;

    public string? MoveToDirectory { get; init; }

    public long MaxFileSizeBytes { get; init; } = 10L * 1024 * 1024;

    public bool IncludeSubdirectories { get; init; }

    public string? QuarantineDirectory { get; init; }

    /// <summary>Slice D2: how the File Reader splits the file into per-record messages.
    /// Defaults to <see cref="FileReaderSplitMode.None"/> (one message per file).</summary>
    public FileReaderSplitMode SplitMode { get; init; } = FileReaderSplitMode.None;

    /// <summary>Slice D2: regex boundary pattern when <see cref="SplitMode"/> is
    /// <see cref="FileReaderSplitMode.Regex"/>. Ignored for other modes.</summary>
    public string? SplitPattern { get; init; }

    /// <summary>Slice D2 / L2 composition: field separator for
    /// <see cref="FileReaderSplitMode.DelimitedTextRecords"/>. Defaults to <c>,</c>; symbolic
    /// values <c>"tab"</c> / <c>"\\t"</c> / <c>"pipe"</c> are resolved like the transform
    /// stage. Ignored for other modes.</summary>
    public string? DelimitedTextDelimiter { get; init; }

    /// <summary>Slice D2 / L2 composition: when <c>true</c> (default) the first non-blank
    /// row of a <see cref="FileReaderSplitMode.DelimitedTextRecords"/> file is treated as
    /// the header and dropped from the dispatched records.</summary>
    public bool DelimitedTextHasHeaderRow { get; init; } = true;

    /// <summary>Parses parameters with sane defaults; throws <see cref="ArgumentException"/> on invalid input.</summary>
    public static FileReaderParameters Parse(IReadOnlyDictionary<string, string> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        var lookup = new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase);

        if (!lookup.TryGetValue("Directory", out var dir) || string.IsNullOrWhiteSpace(dir))
        {
            throw new ArgumentException("FileReader requires a 'Directory' parameter.", nameof(parameters));
        }

        var pattern = lookup.TryGetValue("FilePattern", out var p) && !string.IsNullOrWhiteSpace(p) ? p : "*";
        var poll = lookup.TryGetValue("PollIntervalSeconds", out var ps) && int.TryParse(ps, out var pi) && pi > 0 ? pi : 5;

        var afterRead = FileReaderAfterReadAction.Delete;
        if (lookup.TryGetValue("AfterRead", out var ar) && Enum.TryParse<FileReaderAfterReadAction>(ar, ignoreCase: true, out var parsed))
        {
            afterRead = parsed;
        }

        var moveTo = lookup.TryGetValue("MoveToDirectory", out var mt) ? mt : null;
        if (afterRead == FileReaderAfterReadAction.MoveTo && string.IsNullOrWhiteSpace(moveTo))
        {
            throw new ArgumentException("FileReader AfterRead=MoveTo requires 'MoveToDirectory'.", nameof(parameters));
        }

        var maxSize = 10L * 1024 * 1024;
        if (lookup.TryGetValue("MaxFileSizeBytes", out var ms) && long.TryParse(ms, out var msl) && msl > 0)
        {
            maxSize = msl;
        }

        var includeSub = lookup.TryGetValue("IncludeSubdirectories", out var iss)
                         && bool.TryParse(iss, out var issb)
                         && issb;

        var quarantine = lookup.TryGetValue("QuarantineDirectory", out var q) ? q : null;

        var splitMode = FileReaderSplitMode.None;
        if (lookup.TryGetValue("SplitMode", out var sm)
            && Enum.TryParse<FileReaderSplitMode>(sm, ignoreCase: true, out var parsedSplit))
        {
            splitMode = parsedSplit;
        }

        var splitPattern = lookup.TryGetValue("SplitPattern", out var sp) ? sp : null;
        if (splitMode == FileReaderSplitMode.Regex && string.IsNullOrWhiteSpace(splitPattern))
        {
            throw new ArgumentException(
                "FileReader SplitMode=Regex requires 'SplitPattern'.",
                nameof(parameters));
        }

        var delimitedDelim = lookup.TryGetValue("DelimitedTextDelimiter", out var dd) ? dd : null;
        var delimitedHeader = !lookup.TryGetValue("DelimitedTextHasHeaderRow", out var dh)
            || !bool.TryParse(dh, out var dhb) || dhb;

        return new FileReaderParameters
        {
            Directory = Path.GetFullPath(dir),
            FilePattern = pattern,
            PollIntervalSeconds = poll,
            AfterRead = afterRead,
            MoveToDirectory = string.IsNullOrWhiteSpace(moveTo) ? null : Path.GetFullPath(moveTo),
            MaxFileSizeBytes = maxSize,
            IncludeSubdirectories = includeSub,
            QuarantineDirectory = string.IsNullOrWhiteSpace(quarantine) ? null : Path.GetFullPath(quarantine),
            SplitMode = splitMode,
            SplitPattern = string.IsNullOrWhiteSpace(splitPattern) ? null : splitPattern,
            DelimitedTextDelimiter = string.IsNullOrWhiteSpace(delimitedDelim) ? null : delimitedDelim,
            DelimitedTextHasHeaderRow = delimitedHeader,
        };
    }
}
