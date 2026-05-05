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
        };
    }
}
