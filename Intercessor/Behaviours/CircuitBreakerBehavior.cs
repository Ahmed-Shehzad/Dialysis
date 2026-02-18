using Intercessor.Abstractions;

using Polly;
using Polly.CircuitBreaker;

namespace Intercessor.Behaviours;

/// <inheritdoc />
public class CircuitBreakerBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerBehavior{TRequest, TResponse}"/> class,
    /// setting up a circuit breaker policy to handle transient failures gracefully.
    /// </summary>
    /// <remarks>
    /// The circuit breaker will open after 3 consecutive exceptions and stay open for 10 seconds.
    /// During the open state, any further requests will fail fast.
    /// Logging is performed on break, reset, and half-open transitions.
    /// </remarks>
    public CircuitBreakerBehavior()
    {
        _circuitBreakerPolicy = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(10)
            );
    }

    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default) => await _circuitBreakerPolicy.ExecuteAsync(next);
}

/// <inheritdoc />
public class CircuitBreakerBehavior<TRequest> : IPipelineBehavior<TRequest> where TRequest : IRequest
{
    private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerBehavior{TRequest}"/> class,
    /// setting up a circuit breaker policy to handle transient failures gracefully.
    /// </summary>
    /// <remarks>
    /// The circuit breaker will open after 3 consecutive exceptions and stay open for 10 seconds.
    /// During the open state, any further requests will fail fast.
    /// Logging is performed on break, reset, and half-open transitions.
    /// </remarks>
    public CircuitBreakerBehavior()
    {
        _circuitBreakerPolicy = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(10)
            );
    }

    /// <inheritdoc />
    public async Task HandleAsync(TRequest request, Func<Task> next, CancellationToken cancellationToken = default) => await _circuitBreakerPolicy.ExecuteAsync(next);
}
