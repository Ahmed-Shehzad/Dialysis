using System.Globalization;

namespace Dialysis.SmartConnect.Inbound.Sftp;

/// <summary>
/// Strongly-typed parameter bag for <see cref="SftpSourceConnector"/>. Hydrated from the kind-
/// specific dictionary supplied by <c>SmartConnect:SourceConnectors:[]</c>.
/// </summary>
public sealed class SftpSourceParameters
{
    public required string Host { get; init; }

    public int Port { get; init; } = 22;

    public required string Username { get; init; }

    /// <summary>Password authentication. Mutually exclusive with <see cref="PrivateKeyPath"/>.</summary>
    public string? Password { get; init; }

    /// <summary>Private-key path on the local filesystem. Mutually exclusive with <see cref="Password"/>.</summary>
    public string? PrivateKeyPath { get; init; }

    /// <summary>Passphrase for <see cref="PrivateKeyPath"/>, if encrypted.</summary>
    public string? PrivateKeyPassphrase { get; init; }

    /// <summary>Remote directory to poll. Required.</summary>
    public required string RemoteDirectory { get; init; }

    /// <summary>Glob-style pattern; defaults to <c>*</c>. Matched client-side against listing names.</summary>
    public string FilePattern { get; init; } = "*";

    /// <summary>Polling interval seconds. Falls back to <see cref="ISchedule"/> if a schedule string is supplied via the raw parameter dictionary.</summary>
    public int PollIntervalSeconds { get; init; } = 30;

    /// <summary>After-read action: <c>delete</c> (default), <c>move</c>, or <c>leave</c>.</summary>
    public SftpAfterReadAction AfterRead { get; init; } = SftpAfterReadAction.Delete;

    /// <summary>Remote directory to move successfully-processed files into when <see cref="AfterRead"/> is <see cref="SftpAfterReadAction.Move"/>.</summary>
    public string? MoveToDirectory { get; init; }

    /// <summary>Max file size in bytes; oversized files are skipped with a warning. Default 16 MiB.</summary>
    public long MaxFileSizeBytes { get; init; } = 16L * 1024 * 1024;

    /// <summary>Per-connect / per-operation timeout in seconds. Default 30.</summary>
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Parses the case-insensitive raw parameter dictionary supplied by
    /// <c>SourceConnectorInstanceOptions.Parameters</c>. Throws <see cref="ArgumentException"/>
    /// when required fields are missing or mutually-exclusive auth is misconfigured.
    /// </summary>
    public static SftpSourceParameters Parse(IReadOnlyDictionary<string, string> raw)
    {
        ArgumentNullException.ThrowIfNull(raw);

        string Require(string key) =>
            raw.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v)
                ? v
                : throw new ArgumentException($"SFTP source connector parameter '{key}' is required.");

        string? Optional(string key) =>
            raw.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : null;

        int Int(string key, int fallback) =>
            raw.TryGetValue(key, out var v) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;

        long Long(string key, long fallback) =>
            raw.TryGetValue(key, out var v) && long.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;

        var password = Optional(nameof(Password));
        var privateKey = Optional(nameof(PrivateKeyPath));
        if (password is null && privateKey is null)
        {
            throw new ArgumentException("SFTP source connector requires either Password or PrivateKeyPath.", nameof(raw));
        }
        if (password is not null && privateKey is not null)
        {
            throw new ArgumentException("SFTP source connector accepts Password OR PrivateKeyPath, not both.", nameof(raw));
        }

        var afterReadRaw = Optional(nameof(AfterRead));
        var afterRead = afterReadRaw switch
        {
            null => SftpAfterReadAction.Delete,
            var s when s.Equals("delete", StringComparison.OrdinalIgnoreCase) => SftpAfterReadAction.Delete,
            var s when s.Equals("move", StringComparison.OrdinalIgnoreCase) => SftpAfterReadAction.Move,
            var s when s.Equals("leave", StringComparison.OrdinalIgnoreCase) => SftpAfterReadAction.Leave,
            _ => throw new ArgumentException($"SFTP source connector AfterRead '{afterReadRaw}' is not recognised (delete|move|leave).", nameof(raw)),
        };

        if (afterRead == SftpAfterReadAction.Move && Optional(nameof(MoveToDirectory)) is null)
        {
            throw new ArgumentException("SFTP source connector MoveToDirectory is required when AfterRead=move.", nameof(raw));
        }

        return new SftpSourceParameters
        {
            Host = Require(nameof(Host)),
            Port = Int(nameof(Port), 22),
            Username = Require(nameof(Username)),
            Password = password,
            PrivateKeyPath = privateKey,
            PrivateKeyPassphrase = Optional(nameof(PrivateKeyPassphrase)),
            RemoteDirectory = Require(nameof(RemoteDirectory)),
            FilePattern = Optional(nameof(FilePattern)) ?? "*",
            PollIntervalSeconds = Int(nameof(PollIntervalSeconds), 30),
            AfterRead = afterRead,
            MoveToDirectory = Optional(nameof(MoveToDirectory)),
            MaxFileSizeBytes = Long(nameof(MaxFileSizeBytes), 16L * 1024 * 1024),
            TimeoutSeconds = Int(nameof(TimeoutSeconds), 30),
        };
    }
}

public enum SftpAfterReadAction
{
    Delete,
    Move,
    Leave,
}
