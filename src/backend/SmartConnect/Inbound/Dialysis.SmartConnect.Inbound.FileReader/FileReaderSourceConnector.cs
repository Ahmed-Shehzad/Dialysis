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

        // Slice D2: optionally split the file into per-record sub-messages, each tagged
        // with batch context (slice D) so the operator dashboard can group + filter by the
        // originating file. SplitMode=None preserves the historical one-message-per-file
        // behaviour byte-for-byte.
        var records = SplitPayload(bytes, parameters);
        var batchId = info.FullName; // fully-qualified path uniquely identifies the source.
        var batchSource = $"file:{info.Name}";

        for (var i = 0; i < records.Count; i++)
        {
            var recordBytes = records[i];
            var perRecordMetadata = new Dictionary<string, string>(metadata, StringComparer.Ordinal);
            if (records.Count > 1)
            {
                perRecordMetadata[BatchMetadataKeys.BatchId] = batchId;
                perRecordMetadata[BatchMetadataKeys.Sequence] = (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
                perRecordMetadata[BatchMetadataKeys.Total] = records.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
                perRecordMetadata[BatchMetadataKeys.Source] = batchSource;
            }

            var message = context.MessageFactory.Create(
                context.DefaultFlowId,
                recordBytes,
                PayloadFormat.Binary,
                correlationId: null,
                metadata: perRecordMetadata);

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
                context.Logger.LogError(ex, "FileReader '{Name}' dispatch failed for '{Path}' record {Sequence}/{Total}.", context.InstanceName, fullPath, i + 1, records.Count);
                return;
            }

            if (!result.Succeeded)
            {
                context.Logger.LogWarning(
                    "FileReader '{Name}' dispatch returned {Status}: {Error} for '{Path}' record {Sequence}/{Total}.",
                    context.InstanceName,
                    result.SuggestedHttpStatus,
                    result.Error,
                    fullPath,
                    i + 1,
                    records.Count);
                // Quarantine the whole file on any record's failure — partial-success
                // semantics here would silently drop records.
                QuarantineOrLeave(context, parameters, fullPath);
                return;
            }
        }

        ApplyAfterRead(context, parameters, fullPath);
    }

    /// <summary>
    /// Slice D2: produces one byte array per record per the parameters' <see cref="FileReaderSplitMode"/>.
    /// Returns a single-element list (the whole file) when split mode is <see cref="FileReaderSplitMode.None"/>.
    /// </summary>
    internal static IReadOnlyList<byte[]> SplitPayload(byte[] bytes, FileReaderParameters parameters)
    {
        if (parameters.SplitMode == FileReaderSplitMode.None || bytes.Length == 0)
        {
            return [bytes];
        }

        if (parameters.SplitMode == FileReaderSplitMode.DelimitedTextRecords)
        {
            // Slice D2 / L2: stream-parse the CSV and emit one record per data row. The shared
            // `DelimitedTextStreaming` helper reads via `StreamReader.ReadLine`, so memory stays
            // ~ 1× file size even for 100k-row drops. Header row (when configured) is dropped.
            return SplitDelimitedText(bytes, parameters);
        }

        var text = System.Text.Encoding.UTF8.GetString(bytes);
        IEnumerable<string> records = parameters.SplitMode switch
        {
            FileReaderSplitMode.Hl7V2 => SplitOnHl7v2(text),
            FileReaderSplitMode.Line => text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries),
            FileReaderSplitMode.Regex => System.Text.RegularExpressions.Regex
                .Split(text, parameters.SplitPattern ?? string.Empty)
                .Where(r => !string.IsNullOrWhiteSpace(r)),
            _ => [text],
        };

        var result = records
            .Select(r => System.Text.Encoding.UTF8.GetBytes(r))
            .Where(b => b.Length > 0)
            .ToArray();
        return result.Length == 0 ? [bytes] : result;
    }

    /// <summary>
    /// Slice D2 / L2 composition: parse a delimited-text file and emit one byte array per
    /// data row (the original line, encoded as UTF-8). The first non-blank row is treated
    /// as the header and dropped when the parameters say so. Returns a single-element
    /// list with the whole file when no data rows survive (so the outer loop's "fan-out
    /// only when records.Count > 1" branch falls back to one-message-per-file behaviour
    /// gracefully for malformed / empty inputs).
    /// </summary>
    private static IReadOnlyList<byte[]> SplitDelimitedText(byte[] bytes, FileReaderParameters parameters)
    {
        var delimiter = !string.IsNullOrEmpty(parameters.DelimitedTextDelimiter)
            ? Dialysis.SmartConnect.DataTypes.DelimitedTextStreaming.ResolveDelimiter(parameters.DelimitedTextDelimiter!)
            : ',';
        var options = new Dialysis.SmartConnect.DataTypes.DelimitedTextStreaming.Options(
            Delimiter: delimiter,
            HasHeaderRow: parameters.DelimitedTextHasHeaderRow,
            TrimWhitespace: true,
            SkipBlankLines: true);

        // The raw line is the natural per-record payload — preserves the on-the-wire shape
        // operators can compare against the source file. We use the streaming reader to skip
        // blanks + drop the header in lockstep, but reread the line from the byte buffer
        // so the payload is bytes-faithful rather than re-serialised JSON.
        using var input = new MemoryStream(bytes, writable: false);
        using var reader = new StreamReader(input, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        if (parameters.DelimitedTextHasHeaderRow)
        {
            // Consume the header row from the reader so subsequent ReadLine calls return data.
            _ = Dialysis.SmartConnect.DataTypes.DelimitedTextStreaming.ReadHeader(reader, options);
        }

        var records = new List<byte[]>();
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            records.Add(System.Text.Encoding.UTF8.GetBytes(line));
        }
        return records.Count == 0 ? [bytes] : records;
    }

    private static IEnumerable<string> SplitOnHl7v2(string text)
    {
        // Each HL7v2 message starts with "MSH|"; the carriage return between messages is
        // part of the previous message's last segment in well-formed files. Split on the
        // boundary preceding "MSH|" while keeping that prefix on each record.
        var anchors = new List<int>();
        for (var i = 0; i + 3 < text.Length; i++)
        {
            if (text[i] == 'M' && text[i + 1] == 'S' && text[i + 2] == 'H' && text[i + 3] == '|')
            {
                // Only treat MSH at the start of the file or after a line break as a boundary;
                // an MSH inside a segment value (rare but possible in free-text fields) shouldn't
                // start a new record.
                if (i == 0 || text[i - 1] == '\r' || text[i - 1] == '\n')
                {
                    anchors.Add(i);
                }
            }
        }

        if (anchors.Count <= 1)
        {
            yield return text;
            yield break;
        }

        for (var i = 0; i < anchors.Count; i++)
        {
            var start = anchors[i];
            var end = i + 1 < anchors.Count ? anchors[i + 1] : text.Length;
            yield return text[start..end];
        }
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
