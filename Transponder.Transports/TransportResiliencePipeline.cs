using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

using Transponder.Transports.Abstractions;

namespace Transponder.Transports;

/// <summary>
/// Builds resilience pipelines for transport operations.
/// </summary>
public static class TransportResiliencePipeline
{
    public static ResiliencePipeline Create(TransportResilienceOptions? options)
    {
        if (options is null || (!options.EnableRetry && !options.EnableCircuitBreaker)) return ResiliencePipeline.Empty;

        var builder = new ResiliencePipelineBuilder();

        if (options.EnableCircuitBreaker) _ = builder.AddCircuitBreaker(CreateCircuitBreakerOptions(options.CircuitBreaker));

        if (options.EnableRetry) _ = builder.AddRetry(CreateRetryOptions(options.Retry));

        return builder.Build();
    }

    public static ResiliencePipeline<HttpResponseMessage> CreateHttpPipeline(TransportResilienceOptions? options)
    {
        if (options is null || (!options.EnableRetry && !options.EnableCircuitBreaker)) return ResiliencePipeline<HttpResponseMessage>.Empty;

        var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();

        if (options.EnableCircuitBreaker) _ = builder.AddCircuitBreaker(CreateCircuitBreakerOptions<HttpResponseMessage>(options.CircuitBreaker));

        if (options.EnableRetry) _ = builder.AddRetry(CreateRetryOptions<HttpResponseMessage>(options.Retry));

        return builder.Build();
    }

    public static ISendTransport WrapSend(ISendTransport transport, ResiliencePipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(pipeline);

        return ReferenceEquals(pipeline, ResiliencePipeline.Empty) ? transport : new ResilientSendTransport(transport, pipeline);
    }

    public static IPublishTransport WrapPublish(IPublishTransport transport, ResiliencePipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(pipeline);

        return ReferenceEquals(pipeline, ResiliencePipeline.Empty) ? transport : new ResilientPublishTransport(transport, pipeline);
    }

    private static RetryStrategyOptions CreateRetryOptions(TransportRetryOptions options)
        => new()
        {
            MaxRetryAttempts = options.MaxRetryAttempts,
            Delay = options.Delay,
            BackoffType = options.BackoffType,
            UseJitter = options.UseJitter,
            MaxDelay = options.MaxDelay
        };

    private static RetryStrategyOptions<T> CreateRetryOptions<T>(TransportRetryOptions options)
        => new()
        {
            MaxRetryAttempts = options.MaxRetryAttempts,
            Delay = options.Delay,
            BackoffType = options.BackoffType,
            UseJitter = options.UseJitter,
            MaxDelay = options.MaxDelay
        };

    private static CircuitBreakerStrategyOptions CreateCircuitBreakerOptions(TransportCircuitBreakerOptions options)
        => new()
        {
            FailureRatio = options.FailureRatio,
            MinimumThroughput = options.MinimumThroughput,
            SamplingDuration = options.SamplingDuration,
            BreakDuration = options.BreakDuration
        };

    private static CircuitBreakerStrategyOptions<T> CreateCircuitBreakerOptions<T>(TransportCircuitBreakerOptions options)
        => new()
        {
            FailureRatio = options.FailureRatio,
            MinimumThroughput = options.MinimumThroughput,
            SamplingDuration = options.SamplingDuration,
            BreakDuration = options.BreakDuration
        };
}
