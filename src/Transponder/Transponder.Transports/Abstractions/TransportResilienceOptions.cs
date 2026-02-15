using Polly;

namespace Transponder.Transports.Abstractions;

/// <summary>
/// Defines retry and circuit breaker settings for transport operations.
/// </summary>
public sealed class TransportResilienceOptions
{
    public bool EnableRetry { get; init; } = true;

    public bool EnableCircuitBreaker { get; init; } = true;

    public TransportRetryOptions Retry { get; init; } = new();

    public TransportCircuitBreakerOptions CircuitBreaker { get; init; } = new();
}

/// <summary>
/// Retry settings for transport operations.
/// </summary>
public sealed class TransportRetryOptions
{
    public int MaxRetryAttempts { get; init; } = 3;

    public TimeSpan Delay { get; init; } = TimeSpan.FromMilliseconds(200);

    public DelayBackoffType BackoffType { get; init; } = DelayBackoffType.Exponential;

    public bool UseJitter { get; init; } = true;

    public TimeSpan? MaxDelay { get; init; }
}

/// <summary>
/// Circuit breaker settings for transport operations.
/// </summary>
public sealed class TransportCircuitBreakerOptions
{
    public double FailureRatio { get; init; } = 0.5;

    public int MinimumThroughput { get; init; } = 10;

    public TimeSpan SamplingDuration { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan BreakDuration { get; init; } = TimeSpan.FromSeconds(10);
}
