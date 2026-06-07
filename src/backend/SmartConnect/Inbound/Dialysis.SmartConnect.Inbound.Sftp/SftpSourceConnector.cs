using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace Dialysis.SmartConnect.Inbound.Sftp;

/// <summary>
/// SFTP source connector — polls a remote SFTP directory on a schedule, downloads each matching
/// file, dispatches it as one <see cref="IntegrationMessage"/>, and applies an after-read action
/// (delete / move / leave). Built atop SSH.NET so authentication supports password or private-key.
///
/// Wire by registering this connector via
/// <c>services.AddSmartConnectSftpInbound()</c> in the host. Multiple SFTP instances can be
/// declared via <c>SmartConnect:SourceConnectors:[]</c> with <c>Kind=sftp</c>; each instance opens
/// its own connection and dispatches into its <c>DefaultFlowId</c>. The instances run concurrently
/// under <see cref="Hosting.SourceConnectorHostedService"/>.
/// </summary>
public sealed class SftpSourceConnector : ISourceConnector
{
    public const string KindValue = "sftp";

    public const string MetadataPrefix = "smartconnect.source.sftp.";

    public string Kind => KindValue;

    public async Task RunAsync(SourceConnectorContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        SftpSourceParameters parameters;
        try
        {
            parameters = SftpSourceParameters.Parse(context.Parameters);
        }
        catch (ArgumentException ex)
        {
            context.Logger.LogError(ex, "Sftp '{Name}' has invalid parameters; not starting.", context.InstanceName);
            return;
        }

        context.Logger.LogInformation(
            "Sftp '{Name}' polling sftp://{Host}:{Port}{RemoteDir} (pattern '{Pattern}', interval {Interval}s, afterRead {AfterRead}).",
            context.InstanceName,
            parameters.Host,
            parameters.Port,
            parameters.RemoteDirectory,
            parameters.FilePattern,
            parameters.PollIntervalSeconds,
            parameters.AfterRead);

        var interval = TimeSpan.FromSeconds(parameters.PollIntervalSeconds);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await PollOnceAsync(context, parameters, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // One bad poll shouldn't kill the connector — log and re-try on next interval.
                    context.Logger.LogWarning(ex, "Sftp '{Name}' poll iteration failed.", context.InstanceName);
                }

                await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown
        }
    }

    private static async Task PollOnceAsync(SourceConnectorContext context, SftpSourceParameters p, CancellationToken cancellationToken)
    {
        using var client = BuildClient(p);
        client.OperationTimeout = TimeSpan.FromSeconds(p.TimeoutSeconds);
        try
        {
            client.Connect();
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "Sftp '{Name}' connection failed.", context.InstanceName);
            return;
        }

        try
        {
            // SSH.NET's ListDirectory is synchronous; the operation timeout above bounds it.
            IEnumerable<ISftpFile> entries;
            try
            {
                entries = client.ListDirectory(p.RemoteDirectory);
            }
            catch (Exception ex)
            {
                context.Logger.LogWarning(ex, "Sftp '{Name}' list directory failed for '{Dir}'.", context.InstanceName, p.RemoteDirectory);
                return;
            }

            var glob = ToRegex(p.FilePattern);
            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (entry.IsDirectory || entry.IsSymbolicLink || entry.Name is "." or "..")
                {
                    continue;
                }
                if (!glob.IsMatch(entry.Name))
                {
                    continue;
                }
                if (entry.Length > p.MaxFileSizeBytes)
                {
                    context.Logger.LogWarning(
                        "Sftp '{Name}' skipping oversized '{Path}' ({Length} > {Max}).",
                        context.InstanceName, entry.FullName, entry.Length, p.MaxFileSizeBytes);
                    continue;
                }

                await ProcessEntryAsync(context, client, entry, p, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            try
            { client.Disconnect(); }
            catch { /* best-effort */ }
        }
    }

    private static async Task ProcessEntryAsync(
        SourceConnectorContext context,
        SftpClient client,
        ISftpFile entry,
        SftpSourceParameters p,
        CancellationToken cancellationToken)
    {
        byte[] bytes;
        try
        {
            using var ms = new MemoryStream();
            client.DownloadFile(entry.FullName, ms);
            bytes = ms.ToArray();
        }
        catch (Exception ex)
        {
            context.Logger.LogWarning(ex, "Sftp '{Name}' download failed for '{Path}'.", context.InstanceName, entry.FullName);
            return;
        }

        var sourceMap = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["originalFilename"] = entry.Name,
            ["remoteDirectory"] = p.RemoteDirectory,
            ["fileSize"] = entry.Length,
            ["fileLastModified"] = entry.LastWriteTimeUtc.ToString("O", CultureInfo.InvariantCulture),
        };

        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [MetadataPrefix + "host"] = p.Host,
            [MetadataPrefix + "port"] = p.Port.ToString(CultureInfo.InvariantCulture),
            [MetadataPrefix + "path"] = entry.FullName,
            [MetadataPrefix + "name"] = entry.Name,
            [MetadataPrefix + "sizeBytes"] = entry.Length.ToString(CultureInfo.InvariantCulture),
            [MetadataPrefix + "lastWriteUtc"] = entry.LastWriteTimeUtc.ToString("O", CultureInfo.InvariantCulture),
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
            context.Logger.LogError(ex, "Sftp '{Name}' dispatch failed for '{Path}'.", context.InstanceName, entry.FullName);
            return;
        }

        if (!result.Succeeded)
        {
            context.Logger.LogWarning(
                "Sftp '{Name}' dispatch returned {Status}: {Error} for '{Path}'.",
                context.InstanceName, result.SuggestedHttpStatus, result.Error, entry.FullName);
            // Leave the file in place on dispatch failure so the next poll re-tries.
            return;
        }

        ApplyAfterRead(context, client, entry, p);
    }

    private static void ApplyAfterRead(SourceConnectorContext context, SftpClient client, ISftpFile entry, SftpSourceParameters p)
    {
        try
        {
            switch (p.AfterRead)
            {
                case SftpAfterReadAction.Delete:
                    client.DeleteFile(entry.FullName);
                    break;
                case SftpAfterReadAction.Move when p.MoveToDirectory is not null:
                    var destination = $"{p.MoveToDirectory.TrimEnd('/')}/{entry.Name}";
                    client.RenameFile(entry.FullName, destination);
                    break;
                case SftpAfterReadAction.Leave:
                    break;
            }
        }
        catch (Exception ex)
        {
            context.Logger.LogWarning(ex, "Sftp '{Name}' after-read action {Action} failed for '{Path}'.",
                context.InstanceName, p.AfterRead, entry.FullName);
        }
    }

    private static SftpClient BuildClient(SftpSourceParameters p)
    {
        if (p.PrivateKeyPath is not null)
        {
            var keyFile = p.PrivateKeyPassphrase is null
                ? new PrivateKeyFile(p.PrivateKeyPath)
                : new PrivateKeyFile(p.PrivateKeyPath, p.PrivateKeyPassphrase);
            return new SftpClient(new ConnectionInfo(p.Host, p.Port, p.Username,
                new PrivateKeyAuthenticationMethod(p.Username, keyFile)));
        }

        return new SftpClient(p.Host, p.Port, p.Username, p.Password!);
    }

    /// <summary>
    /// Converts a glob-style pattern (<c>*</c>, <c>?</c>) into an anchored regex. SSH.NET's
    /// <c>ListDirectory</c> does no filtering, so we match client-side.
    /// </summary>
    internal static Regex ToRegex(string globPattern)
    {
        var escaped = Regex.Escape(globPattern)
            .Replace("\\*", ".*", StringComparison.Ordinal)
            .Replace("\\?", ".", StringComparison.Ordinal);
        return new Regex($"^{escaped}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
