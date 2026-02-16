using Dialysis.HisIntegration.Features.Hl7Streaming;
using Dialysis.HisIntegration.Services;
using Dialysis.TestUtilities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Dialysis.HisIntegration.UnitTests;

public sealed class Hl7StreamIngestHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_writer_result()
    {
        var writer = Substitute.For<IHl7StreamingWriter>();
        var writerResult = new Hl7StreamingResult
        {
            Processed = true,
            PatientId = "p1",
            EncounterId = "e1",
            ResourceIds = ["Patient/p1", "Encounter/e1"],
            Error = null
        };
        writer.ConvertAndPersistAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(writerResult);

        var handler = new Hl7StreamIngestHandler(writer);
        var cmd = BogusFakers.Hl7StreamIngestCommandFaker().Generate();

        var result = await handler.HandleAsync(cmd);

        result.Processed.ShouldBeTrue();
        result.PatientId.ShouldBe("p1");
        result.EncounterId.ShouldBe("e1");
        result.ResourceIds.ShouldBe(["Patient/p1", "Encounter/e1"]);
        result.Error.ShouldBeNull();
        await writer.Received(1).ConvertAndPersistAsync(cmd.RawMessage, cmd.MessageType, cmd.TenantId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_unprocessed_returns_error()
    {
        var writer = Substitute.For<IHl7StreamingWriter>();
        writer.ConvertAndPersistAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Hl7StreamingResult { Processed = false, Error = "Conversion failed" });

        var handler = new Hl7StreamIngestHandler(writer);
        var cmd = new Hl7StreamIngestCommand { RawMessage = "invalid", MessageType = "ADT_A01", TenantId = "default" };

        var result = await handler.HandleAsync(cmd);

        result.Processed.ShouldBeFalse();
        result.Error.ShouldBe("Conversion failed");
    }

    [Fact]
    public async Task HandleAsync_passes_tenant_id_to_writer()
    {
        var writer = Substitute.For<IHl7StreamingWriter>();
        writer.ConvertAndPersistAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Hl7StreamingResult { Processed = true });

        var handler = new Hl7StreamIngestHandler(writer);
        var cmd = new Hl7StreamIngestCommand { RawMessage = "MSH|^~\\&|HIS", MessageType = "ADT_A01", TenantId = "tenant-1" };

        await handler.HandleAsync(cmd);

        await writer.Received(1).ConvertAndPersistAsync(Arg.Any<string>(), "ADT_A01", "tenant-1", Arg.Any<CancellationToken>());
    }
}
