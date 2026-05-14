using System.Collections.Immutable;
using System.Text;
using Dialysis.SmartConnect.Inbound;
using Dialysis.SmartConnect.Inbound.FileReader;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class FileReaderSourceConnectorTests : IDisposable
{
    private readonly string _root ;

    public FileReaderSourceConnectorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "sc_filereader_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    private sealed class StubFactory : IInboundMessageFactory
    {
        public IntegrationMessage Create(
            Guid flowId,
            ReadOnlyMemory<byte> payload,
            PayloadFormat format,
            string? correlationId = null,
            IReadOnlyDictionary<string, string>? metadata = null,
            DateTimeOffset? receivedAtUtc = null) =>
            new()
            {
                Id = Guid.NewGuid(),
                FlowId = flowId,
                CorrelationId = correlationId ?? Guid.NewGuid().ToString("N"),
                Payload = payload,
                PayloadFormat = format,
                Metadata = metadata is null
                    ? ImmutableDictionary<string, string>.Empty
                    : metadata.ToImmutableDictionary(StringComparer.Ordinal),
                ReceivedAtUtc = receivedAtUtc ?? DateTimeOffset.UtcNow,
            };
    }

    private SourceConnectorContext BuildContext(
        IReadOnlyDictionary<string, string> parameters,
        List<IntegrationMessage> dispatched,
        bool succeed = true) =>
        new(
            instanceName: "test",
            defaultFlowId: Guid.NewGuid(),
            parameters: parameters,
            messageFactory: new StubFactory(),
            dispatchAsync: (msg, ct) =>
            {
                dispatched.Add(msg);
                return Task.FromResult(new InboundReceiveResult { Succeeded = succeed, Error = succeed ? null : "boom" });
            },
            logger: NullLogger.Instance);

    [Fact]
    public async Task Reads_File_Dispatches_And_Deletes_By_Default_Async()
    {
        var inputPath = Path.Combine(_root, "msg.txt");
        await File.WriteAllTextAsync(inputPath, "hello");

        var dispatched = new List<IntegrationMessage>();
        var ctx = BuildContext(
            new Dictionary<string, string>
            {
                ["Directory"] = _root,
                ["FilePattern"] = "*.txt",
                ["AfterRead"] = "Delete",
            },
            dispatched);

        await FileReaderSourceConnector.PollOnceAsync(ctx, FileReaderParameters.Parse(ctx.Parameters), CancellationToken.None);

        Assert.Single(dispatched);
        Assert.Equal("hello", Encoding.UTF8.GetString(dispatched[0].Payload.Span));
        Assert.Contains(FileReaderSourceConnector.MetadataPrefix + "name", dispatched[0].Metadata.Keys);
        Assert.False(File.Exists(inputPath));
    }

    [Fact]
    public async Task Moveto_Archives_File_After_Dispatch_Async()
    {
        var inputPath = Path.Combine(_root, "moveme.txt");
        var archive = Path.Combine(_root, "archive");
        Directory.CreateDirectory(archive);
        await File.WriteAllTextAsync(inputPath, "x");

        var dispatched = new List<IntegrationMessage>();
        var ctx = BuildContext(
            new Dictionary<string, string>
            {
                ["Directory"] = _root,
                ["FilePattern"] = "moveme.txt",
                ["AfterRead"] = "MoveTo",
                ["MoveToDirectory"] = archive,
            },
            dispatched);

        await FileReaderSourceConnector.PollOnceAsync(ctx, FileReaderParameters.Parse(ctx.Parameters), CancellationToken.None);

        Assert.False(File.Exists(inputPath));
        Assert.True(File.Exists(Path.Combine(archive, "moveme.txt")));
    }

    [Fact]
    public async Task Dispatch_Failure_Quarantines_When_Configured_Async()
    {
        var inputPath = Path.Combine(_root, "bad.txt");
        var quarantine = Path.Combine(_root, "q");
        Directory.CreateDirectory(quarantine);
        await File.WriteAllTextAsync(inputPath, "x");

        var dispatched = new List<IntegrationMessage>();
        var ctx = BuildContext(
            new Dictionary<string, string>
            {
                ["Directory"] = _root,
                ["AfterRead"] = "Delete",
                ["QuarantineDirectory"] = quarantine,
            },
            dispatched,
            succeed: false);

        await FileReaderSourceConnector.PollOnceAsync(ctx, FileReaderParameters.Parse(ctx.Parameters), CancellationToken.None);

        Assert.False(File.Exists(inputPath));
        Assert.True(File.Exists(Path.Combine(quarantine, "bad.txt")));
    }

    [Fact]
    public async Task Oversize_File_Is_Quarantined_And_Not_Dispatched_Async()
    {
        var inputPath = Path.Combine(_root, "huge.bin");
        var quarantine = Path.Combine(_root, "q2");
        Directory.CreateDirectory(quarantine);
        await File.WriteAllBytesAsync(inputPath, new byte[2048]);

        var dispatched = new List<IntegrationMessage>();
        var ctx = BuildContext(
            new Dictionary<string, string>
            {
                ["Directory"] = _root,
                ["MaxFileSizeBytes"] = "16",
                ["QuarantineDirectory"] = quarantine,
            },
            dispatched);

        await FileReaderSourceConnector.PollOnceAsync(ctx, FileReaderParameters.Parse(ctx.Parameters), CancellationToken.None);

        Assert.Empty(dispatched);
        Assert.False(File.Exists(inputPath));
        Assert.True(File.Exists(Path.Combine(quarantine, "huge.bin")));
    }
}
