using System.Text.Json;
using Dialysis.SmartConnect.Scheduling;
using Microsoft.Extensions.Logging;

namespace Dialysis.SmartConnect.Inbound.FileReader;

/// <summary>
/// Mirth-equivalent File Reader source connector: polls a directory at a configured interval,
/// reads each matching file, dispatches it as one <see cref="IntegrationMessage"/>, then deletes
/// or archives the file according to <see cref="FileReaderAfterReadAction"/>.
/// </summary>
public sealed class FileReaderSourceConnector : ISourceConnector
{
    public const string KindValue = "file-reader";

    /// <summary>Metadata key prefix populated for each dispatched file.</summary>
    public const string MetadataPrefix = "smartconnect.source.file.";

    public string Kind => KindValue;

    public async Task RunAsync(SourceConnectorContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        FileReaderParameters parameters;
        try
        {
            parameters = FileReaderParameters.Parse(context.Parameters);
        }
        catch (ArgumentException ex)
        {
            context.Logger.LogError(ex, "FileReader '{Name}' has invalid parameters; not starting.", context.InstanceName);
            return;
        }

        if (!Directory.Exists(parameters.Directory))
        {
            try
            {
                Directory.CreateDirectory(parameters.Directory);
            }
            catch (Exception ex)
            {
                context.Logger.LogError(
                    ex,
                    "FileReader '{Name}' cannot create poll directory '{Directory}'; not starting.",
                    context.InstanceName,
                    parameters.Directory);
                return;
            }
        }

        if (parameters.AfterRead == FileReaderAfterReadAction.MoveTo && parameters.MoveToDirectory is not null)
        {
            Directory.CreateDirectory(parameters.MoveToDirectory);
        }

        if (parameters.QuarantineDirectory is not null)
        {
            Directory.CreateDirectory(parameters.QuarantineDirectory);
        }

        ISchedule schedule;
        try
        {
            schedule = ScheduleFactory.FromParameters(context.Parameters, parameters.PollIntervalSeconds);
        }
        catch (ArgumentException ex)
        {
            context.Logger.LogError(ex, "FileReader '{Name}' has invalid schedule; not starting.", context.InstanceName);
            return;
        }

        context.Logger.LogInformation(
            "FileReader '{Name}' polling '{Directory}' (pattern '{Pattern}', schedule {Schedule}, afterRead {AfterRead}).",
            context.InstanceName,
            parameters.Directory,
            parameters.FilePattern,
            schedule.GetType().Name,
            parameters.AfterRead);

        // Run an initial pass immediately, then follow the schedule.
        await PollOnceAsync(context, parameters, cancellationToken).ConfigureAwait(false);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var now = DateTimeOffset.UtcNow;
                var next = schedule.NextOccurrence(now);
                if (next is null)
                {
                    context.Logger.LogInformation(
                        "FileReader '{Name}' schedule has no future occurrence; stopping.",
                        context.InstanceName);
                    break;
                }

                var delay = next.Value - now;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }

                await PollOnceAsync(context, parameters, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown
        }
    }

    internal static async Task PollOnceAsync(
        SourceConnectorContext context,
        FileReaderParameters parameters,
        CancellationToken cancellationToken)
    {
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(
                parameters.Directory,
                parameters.FilePattern,
                parameters.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }
        catch (DirectoryNotFoundException)
        {
            return;
        }
        catch (Exception ex)
        {
            context.Logger.LogWarning(ex, "FileReader '{Name}' enumeration failed.", context.InstanceName);
            return;
        }

        foreach (var rawPath in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(rawPath);
            }
            catch (Exception ex)
            {
                context.Logger.LogWarning(ex, "FileReader '{Name}' could not resolve '{Path}'.", context.InstanceName, rawPath);
                continue;
            }

            if (!IsContained(parameters.Directory, fullPath))
            {
                context.Logger.LogWarning(
                    "FileReader '{Name}' skipping path outside poll directory: '{Path}'.",
                    context.InstanceName,
                    fullPath);
                continue;
            }

            await ProcessFileAsync(context, parameters, fullPath, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task ProcessFileAsync(
        SourceConnectorContext context,
        FileReaderParameters parameters,
        string fullPath,
        CancellationToken cancellationToken)
    {
        FileInfo info;
        try
        {
            info = new FileInfo(fullPath);
            if (!info.Exists)
            {
                return;
            }
        }
        catch (Exception ex)
        {
            context.Logger.LogWarning(ex, "FileReader '{Name}' stat failed for '{Path}'.", context.InstanceName, fullPath);
            return;
        }

        if (info.Length > parameters.MaxFileSizeBytes)
        {
            context.Logger.LogWarning(
                "FileReader '{Name}' rejecting oversized file '{Path}' ({Length} bytes > {Max}).",
                context.InstanceName,
                fullPath,
                info.Length,
                parameters.MaxFileSizeBytes);
            QuarantineOrLeave(context, parameters, fullPath);
            return;
        }

        byte[] bytes;
        try
        {
            await using var stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);
            bytes = new byte[stream.Length];
            var offset = 0;
            while (offset < bytes.Length)
            {
                var read = await stream.ReadAsync(bytes.AsMemory(offset, bytes.Length - offset), cancellationToken)
                    .ConfigureAwait(false);
                if (read <= 0)
                {
                    break;
                }

                offset += read;
            }

            if (offset != bytes.Length)
            {
                Array.Resize(ref bytes, offset);
            }
        }
        catch (IOException ex)
        {
            // File may be locked by a writer; try again on the next tick.
            context.Logger.LogDebug(ex, "FileReader '{Name}' could not open '{Path}' yet.", context.InstanceName, fullPath);
            return;
        }
        catch (Exception ex)
        {
            context.Logger.LogWarning(ex, "FileReader '{Name}' read failed for '{Path}'.", context.InstanceName, fullPath);
            return;
        }

        var sizeBytes = info.Length.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var lastWriteUtc = info.LastWriteTimeUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture);

        var sourceMap = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["originalFilename"] = info.Name,
            ["fileDirectory"] = Path.GetDirectoryName(fullPath) ?? string.Empty,
            ["fileSize"] = info.Length,
            ["fileLastModified"] = lastWriteUtc,
        };

        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [MetadataPrefix + "path"] = fullPath,
            [MetadataPrefix + "name"] = info.Name,
            [MetadataPrefix + "sizeBytes"] = sizeBytes,
            [MetadataPrefix + "lastWriteUtc"] = lastWriteUtc,
            ["smartconnect.sourcemap.json"] = JsonSerializer.Serialize(sourceMap),
        };

        var message = context.MessageFactory.Create(
            context.DefaultFlowId,
            bytes,
            PayloadFormat.Binary,
            correlationId: null,
            metadata: metadata);

        InboundReceiveResult result;
        try
        {
            result = await context.DispatchAsync(message, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "FileReader '{Name}' dispatch failed for '{Path}'.", context.InstanceName, fullPath);
            return;
        }

        if (!result.Succeeded)
        {
            context.Logger.LogWarning(
                "FileReader '{Name}' dispatch returned {Status}: {Error} for '{Path}'.",
                context.InstanceName,
                result.SuggestedHttpStatus,
                result.Error,
                fullPath);
            // Leave the file in place so it can be retried on the next tick or moved to quarantine
            // if configured. We deliberately do not delete on dispatch failure.
            QuarantineOrLeave(context, parameters, fullPath);
            return;
        }

        ApplyAfterRead(context, parameters, fullPath);
    }

    private static void ApplyAfterRead(
        SourceConnectorContext context,
        FileReaderParameters parameters,
        string fullPath)
    {
        try
        {
            switch (parameters.AfterRead)
            {
                case FileReaderAfterReadAction.Delete:
                    File.Delete(fullPath);
                    break;
                case FileReaderAfterReadAction.MoveTo when parameters.MoveToDirectory is not null:
                    var destination = Path.Combine(parameters.MoveToDirectory, Path.GetFileName(fullPath));
                    destination = EnsureUniquePath(destination);
                    File.Move(fullPath, destination);
                    break;
                case FileReaderAfterReadAction.Leave:
                    break;
            }
        }
        catch (Exception ex)
        {
            context.Logger.LogWarning(
                ex,
                "FileReader '{Name}' post-dispatch action {AfterRead} failed for '{Path}'.",
                context.InstanceName,
                parameters.AfterRead,
                fullPath);
        }
    }

    private static void QuarantineOrLeave(
        SourceConnectorContext context,
        FileReaderParameters parameters,
        string fullPath)
    {
        if (parameters.QuarantineDirectory is null)
        {
            return;
        }

        try
        {
            var destination = Path.Combine(parameters.QuarantineDirectory, Path.GetFileName(fullPath));
            destination = EnsureUniquePath(destination);
            File.Move(fullPath, destination);
        }
        catch (Exception ex)
        {
            context.Logger.LogWarning(
                ex,
                "FileReader '{Name}' could not quarantine '{Path}'.",
                context.InstanceName,
                fullPath);
        }
    }

    private static string EnsureUniquePath(string candidate)
    {
        if (!File.Exists(candidate))
        {
            return candidate;
        }

        var dir = Path.GetDirectoryName(candidate)!;
        var name = Path.GetFileNameWithoutExtension(candidate);
        var ext = Path.GetExtension(candidate);
        for (var i = 1; i < 1000; i++)
        {
            var attempt = Path.Combine(dir, $"{name}.{i}{ext}");
            if (!File.Exists(attempt))
            {
                return attempt;
            }
        }

        return Path.Combine(dir, $"{name}.{Guid.NewGuid():N}{ext}");
    }

    private static bool IsContained(string root, string candidate)
    {
        var rootFull = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var candidateFull = Path.GetFullPath(candidate);
        return candidateFull.StartsWith(
            rootFull + Path.DirectorySeparatorChar,
            StringComparison.Ordinal)
            || string.Equals(
                Path.TrimEndingDirectorySeparator(Path.GetDirectoryName(candidateFull) ?? ""),
                rootFull,
                StringComparison.Ordinal)
            || candidateFull.StartsWith(rootFull, StringComparison.Ordinal);
    }
}
