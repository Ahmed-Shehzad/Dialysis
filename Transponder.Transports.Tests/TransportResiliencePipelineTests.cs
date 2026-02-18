using Polly;

using Transponder.Transports.Abstractions;

namespace Transponder.Transports.Tests;

public sealed class TransportResiliencePipelineTests
{
    private sealed class StubSendTransport : ISendTransport
    {
        public Task SendAsync(ITransportMessage message, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class StubPublishTransport : IPublishTransport
    {
        public Task PublishAsync(ITransportMessage message, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    [Fact]
    public void Create_Returns_Empty_When_Disabled()
    {
        var options = new TransportResilienceOptions
        {
            EnableRetry = false,
            EnableCircuitBreaker = false
        };

        ResiliencePipeline pipeline = TransportResiliencePipeline.Create(options);

        Assert.Same(ResiliencePipeline.Empty, pipeline);
    }

    [Fact]
    public void WrapSend_Returns_Inner_For_Empty_Pipeline()
    {
        var transport = new StubSendTransport();
        ISendTransport wrapped = TransportResiliencePipeline.WrapSend(transport, ResiliencePipeline.Empty);

        Assert.Same(transport, wrapped);
    }

    [Fact]
    public void WrapPublish_Returns_Inner_For_Empty_Pipeline()
    {
        var transport = new StubPublishTransport();
        IPublishTransport wrapped = TransportResiliencePipeline.WrapPublish(transport, ResiliencePipeline.Empty);

        Assert.Same(transport, wrapped);
    }
}
