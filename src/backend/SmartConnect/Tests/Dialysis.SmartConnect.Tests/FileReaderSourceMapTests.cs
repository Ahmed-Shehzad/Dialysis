using System.Text.Json;
using Dialysis.SmartConnect.Inbound;
using Dialysis.SmartConnect.Inbound.FileReader;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class FileReaderSourceMapTests
{
    [Fact]
    public async Task Pollonce_Dispatches_Message_With_Originalfilename_In_Sourcemap_Metadata_Async()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"sc_sourcemap_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, "patient-42.txt");
        await File.WriteAllTextAsync(filePath, "payload-bytes");

        try
        {
            IntegrationMessage? captured = null;
            var ctx = new SourceConnectorContext(
                instanceName: "test",
                defaultFlowId: Guid.NewGuid(),
                parameters: new Dictionary<string, string>
                {
                    ["directory"] = dir,
                    ["filePattern"] = "*.txt",
                    ["pollIntervalSeconds"] = "1",
                    ["afterRead"] = "Leave",
                },
                messageFactory: new TestInboundMessageFactory(),
                dispatchAsync: (msg, _) =>
                {
                    captured = msg;
                    return Task.FromResult(new InboundReceiveResult { Succeeded = true });
                },
                logger: NullLogger.Instance);

            var parameters = FileReaderParameters.Parse(ctx.Parameters);
            await FileReaderSourceConnector.PollOnceAsync(ctx, parameters, CancellationToken.None);

            Assert.NotNull(captured);
            Assert.True(captured!.Metadata.TryGetValue("smartconnect.sourcemap.json", out var sourceMapJson));
            using var doc = JsonDocument.Parse(sourceMapJson!);
            Assert.Equal("patient-42.txt", doc.RootElement.GetProperty("originalFilename").GetString());
            Assert.Equal(13, doc.RootElement.GetProperty("fileSize").GetInt64());
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { }
        }
    }

    private sealed class TestInboundMessageFactory : IInboundMessageFactory
    {
        public IntegrationMessage Create(
            Guid flowId,
            ReadOnlyMemory<byte> payload,
            PayloadFormat payloadFormat,
            string? correlationId,
            IReadOnlyDictionary<string, string>? metadata = null,
            DateTimeOffset? receivedAtUtc = null)
        {
            var meta = System.Collections.Immutable.ImmutableDictionary<string, string>.Empty;
            if (metadata is not null)
            {
                foreach (var kv in metadata) meta = meta.SetItem(kv.Key, kv.Value);
            }

            return new IntegrationMessage
            {
                Id = Guid.CreateVersion7(),
                FlowId = flowId,
                CorrelationId = correlationId ?? Guid.NewGuid().ToString("N"),
                Payload = payload,
                PayloadFormat = payloadFormat,
                Metadata = meta,
                ReceivedAtUtc = receivedAtUtc ?? DateTimeOffset.UtcNow,
            };
        }
    }
}
