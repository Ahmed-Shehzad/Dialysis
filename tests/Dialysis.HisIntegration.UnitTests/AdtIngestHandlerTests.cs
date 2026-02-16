using Dialysis.HisIntegration.Features.AdtSync;
using Dialysis.HisIntegration.Services;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Dialysis.HisIntegration.UnitTests;

public sealed class AdtIngestHandlerTests
{
    [Fact]
    public async Task HandleAsync_invalid_message_returns_not_processed()
    {
        var parser = new AdtMessageParser();
        var writer = Substitute.For<IFhirAdtWriter>();
        var provenance = Substitute.For<IProvenanceRecorder>();

        var handler = new AdtIngestHandler(parser, writer, provenance);
        var cmd = new AdtIngestCommand { MessageType = "ADT-A01", RawMessage = "invalid" };

        var result = await handler.HandleAsync(cmd);

        result.Processed.ShouldBeFalse();
        result.Message.ShouldNotBeNull().ShouldContain("Invalid");
        await writer.DidNotReceive().WriteAdtAsync(Arg.Any<AdtParsedData>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_valid_message_calls_writer_and_provenance()
    {
        var parser = new AdtMessageParser();
        var writer = Substitute.For<IFhirAdtWriter>();
        writer.WriteAdtAsync(Arg.Any<AdtParsedData>(), Arg.Any<CancellationToken>())
            .Returns(("patient-1", "encounter-1"));

        var provenance = Substitute.For<IProvenanceRecorder>();

        var handler = new AdtIngestHandler(parser, writer, provenance);
        var lines = new[]
        {
            "MSH|^~\\&|HIS|HOSP|PDMS|CLINIC|20240115120000||ADT^A01|MSG001|P|2.5",
            "PID|1||MRN123^^^HOSP^MR||Doe^John||19800115|M",
            "PV1|1|I|^Ward^|||||^Smith^Jane|||ADM||"
        };
        var cmd = new AdtIngestCommand { MessageType = "ADT-A01", RawMessage = string.Join("\r\n", lines) };

        var result = await handler.HandleAsync(cmd);

        result.Processed.ShouldBeTrue();
        result.PatientId.ShouldBe("patient-1");
        result.EncounterId.ShouldBe("encounter-1");
        await writer.Received(1).WriteAdtAsync(Arg.Any<AdtParsedData>(), Arg.Any<CancellationToken>());
        await provenance.Received(1).RecordAdtProvenanceAsync("patient-1", "Patient", Arg.Any<string>(), Arg.Any<AdtParsedData>(), Arg.Any<CancellationToken>());
        await provenance.Received(1).RecordAdtProvenanceAsync("encounter-1", "Encounter", Arg.Any<string>(), Arg.Any<AdtParsedData>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_writer_returns_null_skips_provenance()
    {
        var parser = new AdtMessageParser();
        var writer = Substitute.For<IFhirAdtWriter>();
        writer.WriteAdtAsync(Arg.Any<AdtParsedData>(), Arg.Any<CancellationToken>())
            .Returns(((string?)null, (string?)null));

        var provenance = Substitute.For<IProvenanceRecorder>();

        var lines = new[]
        {
            "MSH|^~\\&|HIS|HOSP|PDMS|CLINIC|20240115120000||ADT^A01|MSG001|P|2.5",
            "PID|1||MRN123^^^HOSP^MR||Doe^John||19800115|M",
            "PV1|1|I|^Ward^|||||^Smith^Jane|||ADM||"
        };

        var handler = new AdtIngestHandler(parser, writer, provenance);
        var cmd = new AdtIngestCommand { MessageType = "ADT-A01", RawMessage = string.Join("\r\n", lines) };

        var result = await handler.HandleAsync(cmd);

        result.Processed.ShouldBeTrue();
        result.Message.ShouldNotBeNull().ShouldContain("skipped");
        await provenance.DidNotReceive().RecordAdtProvenanceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AdtParsedData>(), Arg.Any<CancellationToken>());
    }
}
