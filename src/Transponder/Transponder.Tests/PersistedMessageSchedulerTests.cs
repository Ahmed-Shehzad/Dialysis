using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Transponder.Abstractions;
using Transponder.Persistence.Abstractions;
using Transponder.Transports.Abstractions;

namespace Transponder.Tests;

public sealed class PersistedMessageSchedulerTests
{
    private readonly TransponderBus _bus;
    private readonly Mock<IScheduledMessageStore> _storeMock;
    private readonly IMessageSerializer _serializer;
    private readonly Mock<ITransportHostProvider> _hostProviderMock;
    private readonly ILogger<PersistedMessageScheduler> _logger;
    private readonly PersistedMessageSchedulerOptions _options;

    public PersistedMessageSchedulerTests()
    {
        _serializer = new JsonMessageSerializer();
        _hostProviderMock = new Mock<ITransportHostProvider>();
        _bus = TestHelpers.CreateTestBus(_serializer, _hostProviderMock.Object);
        _storeMock = new Mock<IScheduledMessageStore>();
        _logger = NullLogger<PersistedMessageScheduler>.Instance;
        _options = new PersistedMessageSchedulerOptions
        {
            PollInterval = TimeSpan.FromMilliseconds(100),
            BatchSize = 10
        };
    }

    [Fact]
    public async Task ScheduleSendAsync_Should_Throw_When_ScheduledTime_In_PastAsync()
    {
        // Arrange
        var scheduler = new PersistedMessageScheduler(
            _bus,
            _storeMock.Object,
            _serializer,
            _hostProviderMock.Object,
            _options,
            _logger);

        var message = new TestMessage();
        var pastTime = DateTimeOffset.UtcNow.AddMinutes(-1);

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await scheduler.ScheduleSendAsync(new Uri("http://test/destination"), message, pastTime));
    }

    [Fact]
    public async Task ScheduleSendAsync_Should_Store_MessageAsync()
    {
        // Arrange
        var scheduler = new PersistedMessageScheduler(
            _bus,
            _storeMock.Object,
            _serializer,
            _hostProviderMock.Object,
            _options,
            _logger);

        var message = new TestMessage();
        var futureTime = DateTimeOffset.UtcNow.AddMinutes(1);

        // Act
        var handle = await scheduler.ScheduleSendAsync(new Uri("http://test/destination"), message, futureTime);

        // Assert
        Assert.NotNull(handle);
        _storeMock.Verify(s => s.AddAsync(It.IsAny<IScheduledMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SchedulePublishAsync_Should_Throw_When_Delay_Is_ZeroAsync()
    {
        // Arrange
        var scheduler = new PersistedMessageScheduler(
            _bus,
            _storeMock.Object,
            _serializer,
            _hostProviderMock.Object,
            _options,
            _logger);

        var message = new TestMessage();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await scheduler.SchedulePublishAsync(message, TimeSpan.Zero));
    }

    private sealed class TestMessage : IMessage
    {
        public TestMessage(Ulid? correlationId = null)
        {
            CorrelationId = correlationId;
        }
        public Ulid? CorrelationId { get; set; }
    }
}
