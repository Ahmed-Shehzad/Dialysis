using Microsoft.Extensions.Logging;

using Moq;

using Transponder.Persistence;
using Transponder.Persistence.Abstractions;
using Transponder.Transports.Abstractions;

namespace Transponder.Tests;

public sealed class DeadLetterQueueTests
{
    [Fact]
    public async Task OutboxDispatcher_Should_Send_Unresolvable_Message_To_DeadLetterQueueAsync()
    {
        // Arrange
        var sessionFactoryMock = new Mock<IStorageSessionFactory>();
        var sessionMock = new Mock<IStorageSession>();
        var outboxMock = new Mock<IOutboxStore>();
        _ = sessionMock.Setup(s => s.Outbox).Returns(outboxMock.Object);
        _ = sessionFactoryMock.Setup(f => f.CreateSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionMock.Object);

        var hostMock = new Mock<ITransportHost>();
        var transportMock = new Mock<ISendTransport>();
        var hostProviderMock = new Mock<ITransportHostProvider>();
        var loggerMock = new Mock<ILogger<OutboxDispatcher>>();

        var deadLetterAddress = new Uri("http://test/dlq");
        _ = hostProviderMock.Setup(h => h.GetHost(deadLetterAddress))
            .Returns(hostMock.Object);
        _ = hostMock.Setup(h => h.GetSendTransportAsync(deadLetterAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transportMock.Object);

        var options = new OutboxDispatchOptions
        {
            ChannelCapacity = 10,
            BatchSize = 5,
            PollInterval = TimeSpan.FromMilliseconds(100),
            RetryDelay = TimeSpan.FromMilliseconds(50),
            MaxConcurrentDestinations = 2,
            DeadLetterAddress = deadLetterAddress
        };

        var dispatcher = new OutboxDispatcher(
            sessionFactoryMock.Object,
            hostProviderMock.Object,
            options,
            loggerMock.Object);

        var message = new OutboxMessage(
            Ulid.NewUlid(),
            new ReadOnlyMemory<byte>([]),
            new OutboxMessageOptions
            {
                MessageType = "NonExistent.Assembly.NonExistentType, NonExistentAssembly",
                ContentType = "application/json",
                DestinationAddress = null,
                SourceAddress = new Uri("http://test/source")
            });

        await dispatcher.StartAsync();

        // Act
        await dispatcher.EnqueueAsync(message);
        await Task.Delay(500);

        // Assert
        transportMock.Verify(
            t => t.SendAsync(
                It.Is<ITransportMessage>(m =>
                    m.Headers.ContainsKey("DeadLetterReason") &&
                    m.Headers["DeadLetterReason"] != null &&
                    m.Headers["DeadLetterReason"]!.ToString() == "UnresolvableMessageType"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        hostProviderMock.Verify(h => h.GetHost(deadLetterAddress), Times.Once);

        await dispatcher.StopAsync();
    }

    [Fact]
    public void PersistedMessageSchedulerOptions_Should_Support_DeadLetterAddress()
    {
        // Arrange
        var options = new PersistedMessageSchedulerOptions();
        var deadLetterAddress = new Uri("http://test/dlq");

        // Act
        options.DeadLetterAddress = deadLetterAddress;

        // Assert
        Assert.NotNull(options.DeadLetterAddress);
        Assert.Equal(deadLetterAddress, options.DeadLetterAddress);
    }
}
