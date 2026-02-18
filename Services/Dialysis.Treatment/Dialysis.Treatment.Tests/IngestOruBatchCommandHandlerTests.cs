using Dialysis.Treatment.Application.Features.IngestOruBatch;
using Dialysis.Treatment.Application.Features.IngestOruMessage;
using Dialysis.Treatment.Infrastructure.Hl7;

using Intercessor.Abstractions;

using Moq;

using Shouldly;

namespace Dialysis.Treatment.Tests;

public class IngestOruBatchCommandHandlerTests
{
    private readonly Hl7BatchParser _batchParser = new();
    private readonly Mock<ISender> _senderMock = new();

    [Fact]
    public async Task HandleAsync_SingleOruMessage_ProcessesAndReturnsOneSessionAsync()
    {
        string oru = "MSH|^~\\&|DEV|FAC|PDMS|FAC|20230215120000||ORU^R01|M001|P|2.5\r"
                     + "PID|||MRN123^^^^MR\r"
                     + "OBR|1||S001||||||||||||\r"
                     + "OBX|1|NM|152348^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE^MDC|1.1.3.1|300|ml/min^ml/min^UCUM\r";
        string batch = $"FHS|^~\\&||||||\rBHS|^~\\&||||||\r{oru}\rBTS|1\rFTS|1\r";

        _ = _senderMock
            .Setup(s => s.SendAsync(It.IsAny<IngestOruMessageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IngestOruMessageResponse("session-001", 1, true));

        var handler = new IngestOruBatchCommandHandler(_batchParser, _senderMock.Object);
        var command = new IngestOruBatchCommand(batch);

        IngestOruBatchResponse response = await handler.HandleAsync(command);

        response.ProcessedCount.ShouldBe(1);
        response.SessionIds.ShouldContain("session-001");
        _senderMock.Verify(
            s => s.SendAsync(It.Is<IngestOruMessageCommand>(c => c.RawHl7Message.Contains("MRN123")), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_TwoOruMessages_ProcessesBothAsync()
    {
        string oru1 = "MSH|^~\\&|DEV|FAC|PDMS|FAC|20230215120000||ORU^R01|M001|P|2.5\rPID|||MRN1\rOBR|1||S001\rOBX|1|NM|X^Y^MDC|1.1.1|100\r";
        string oru2 = "MSH|^~\\&|DEV|FAC|PDMS|FAC|20230215120100||ORU^R01|M002|P|2.5\rPID|||MRN2\rOBR|1||S002\rOBX|1|NM|X^Y^MDC|1.1.1|200\r";
        string batch = $"FHS|^~\\&||||||\rBHS|^~\\&||||||\r{oru1}\r{oru2}\rBTS|2\rFTS|1\r";

        _ = _senderMock
            .Setup(s => s.SendAsync(It.IsAny<IngestOruMessageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IngestOruMessageResponse("session-ok", 1, true));

        var handler = new IngestOruBatchCommandHandler(_batchParser, _senderMock.Object);
        var command = new IngestOruBatchCommand(batch);

        IngestOruBatchResponse response = await handler.HandleAsync(command);

        response.ProcessedCount.ShouldBe(2);
        response.SessionIds.Count.ShouldBe(2);
        _senderMock.Verify(s => s.SendAsync(It.IsAny<IngestOruMessageCommand>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task HandleAsync_NonOruMessage_SkipsWithoutProcessingAsync()
    {
        string qbp = "MSH|^~\\&|DEV|FAC|PDMS|FAC|20230215120000||QBP^D01^QBP_D01|M001|P|2.6\r"
                    + "QPD|MDC_HDIALY_RX_QUERY^Query^MDC|Q001|@PID.3|MRN123^^^^MR\r"
                    + "RCP|I||RD\r";
        string batch = $"FHS|^~\\&||||||\rBHS|^~\\&||||||\r{qbp}\rBTS|1\rFTS|1\r";

        var handler = new IngestOruBatchCommandHandler(_batchParser, _senderMock.Object);
        var command = new IngestOruBatchCommand(batch);

        IngestOruBatchResponse response = await handler.HandleAsync(command);

        response.ProcessedCount.ShouldBe(0);
        response.SessionIds.ShouldBeEmpty();
        _senderMock.Verify(s => s.SendAsync(It.IsAny<IRequest<IngestOruMessageResponse>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_MixedOruAndNonOru_ProcessesOnlyOruMessagesAsync()
    {
        string oru = "MSH|^~\\&|DEV|FAC|PDMS|FAC|20230215120000||ORU^R01|M001|P|2.5\rPID|||MRN1\rOBR|1||S001\rOBX|1|NM|X^Y^MDC|1.1.1|100\r";
        string qbp = "MSH|^~\\&|DEV|FAC|PDMS|FAC|20230215120001||QBP^D01^QBP_D01|M002|P|2.6\rQPD|X|Q002\rRCP|I\r";
        string batch = $"FHS|^~\\&||||||\rBHS|^~\\&||||||\r{oru}\r{qbp}\rBTS|2\rFTS|1\r";

        _ = _senderMock
            .Setup(s => s.SendAsync(It.IsAny<IngestOruMessageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IngestOruMessageResponse("session-001", 1, true));

        var handler = new IngestOruBatchCommandHandler(_batchParser, _senderMock.Object);
        var command = new IngestOruBatchCommand(batch);

        IngestOruBatchResponse response = await handler.HandleAsync(command);

        response.ProcessedCount.ShouldBe(1);
        response.SessionIds.ShouldContain("session-001");
        _senderMock.Verify(s => s.SendAsync(It.IsAny<IngestOruMessageCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
