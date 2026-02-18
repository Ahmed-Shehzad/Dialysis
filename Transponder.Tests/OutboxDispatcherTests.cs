using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Transponder.Persistence;
using Transponder.Persistence.Abstractions;
using Transponder.Transports.Abstractions;

namespace Transponder.Tests;

public sealed class OutboxDispatcherTests
{
    private readonly Mock<IStorageSessionFactory> _sessionFactoryMock;
    private readonly Mock<ITransportHostProvider> _hostProviderMock;
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly OutboxDispatchOptions _options;

    public OutboxDispatcherTests()
    {
        _sessionFactoryMock = new Mock<IStorageSessionFactory>();
        _hostProviderMock = new Mock<ITransportHostProvider>();
        _logger = NullLogger<OutboxDispatcher>.Instance;
        _options = new OutboxDispatchOptions
        {
            ChannelCapacity = 10,
            BatchSize = 5,
            PollInterval = TimeSpan.FromMilliseconds(100),
            RetryDelay = TimeSpan.FromMilliseconds(50),
            MaxConcurrentDestinations = 2
        };
    }

    [Fact]
    public async Task EnqueueAsync_Should_Add_Message_To_OutboxAsync()
    {
        // Arrange
        var sessionMock = new Mock<IStorageSession>();
        var outboxMock = new Mock<IOutboxStore>();
        _ = sessionMock.Setup(s => s.Outbox).Returns(outboxMock.Object);
        _ = _sessionFactoryMock.Setup(f => f.CreateSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionMock.Object);

        var dispatcher = new OutboxDispatcher(
            _sessionFactoryMock.Object,
            _hostProviderMock.Object,
            _options,
            _logger);

        var message = new OutboxMessage(
            Ulid.NewUlid(),
            new ReadOnlyMemory<byte>([]),
            new OutboxMessageOptions
            {
                MessageType = "TestMessage",
                ContentType = "application/json",
                DestinationAddress = new Uri("http://test/destination"),
                SourceAddress = new Uri("http://test/source")
            });

        // Act
        await dispatcher.EnqueueAsync(message);

        // Assert
        outboxMock.Verify(o => o.AddAsync(message, It.IsAny<CancellationToken>()), Times.Once);
        sessionMock.Verify(s => s.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_Should_Start_Dispatch_And_Poll_LoopsAsync()
    {
        // Arrange
        var sessionMock = new Mock<IStorageSession>();
        var outboxMock = new Mock<IOutboxStore>();
        _ = outboxMock.Setup(o => o.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<IOutboxMessage>());
        _ = sessionMock.Setup(s => s.Outbox).Returns(outboxMock.Object);
        _ = _sessionFactoryMock.Setup(f => f.CreateSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionMock.Object);

        var dispatcher = new OutboxDispatcher(
            _sessionFactoryMock.Object,
            _hostProviderMock.Object,
            _options,
            _logger);

        // Act
        await dispatcher.StartAsync();

        // Assert
        await Task.Delay(150); // Wait for poll loop to run

        outboxMock.Verify(o => o.GetPendingAsync(_options.BatchSize, It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        // Cleanup
        await dispatcher.StopAsync();
    }

    [Fact]
    public async Task StopAsync_Should_Complete_Within_TimeoutAsync()
    {
        // Arrange
        var sessionMock = new Mock<IStorageSession>();
        var outboxMock = new Mock<IOutboxStore>();
        _ = outboxMock.Setup(o => o.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<IOutboxMessage>());
        _ = sessionMock.Setup(s => s.Outbox).Returns(outboxMock.Object);
        _ = _sessionFactoryMock.Setup(f => f.CreateSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionMock.Object);

        var dispatcher = new OutboxDispatcher(
            _sessionFactoryMock.Object,
            _hostProviderMock.Object,
            _options,
            _logger);

        await dispatcher.StartAsync();
        await Task.Delay(50);

        // Act
        Task stopTask = dispatcher.StopAsync();
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));

        Task completedTask = await Task.WhenAny(stopTask, timeoutTask);

        // Assert
        Assert.Equal(stopTask, completedTask);
        Assert.True(stopTask.IsCompletedSuccessfully);
    }
}
