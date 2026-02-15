using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;

namespace Intercessor.Behaviours;

/// <inheritdoc />
public class RetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private const int MaxRetries = 5;

    private readonly AsyncRetryPolicy<TResponse> _retryPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryBehavior{TRequest, TResponse}"/> class,
    /// which applies a retry policy with exponential backoff to handle transient failures during request processing.
    /// </summary>
    /// <param name="logger">
    /// The logger used to record retry attempts, including warnings on exceptions and contextual information about the request.
    /// </param>
    public RetryBehavior(ILogger<RetryBehavior<TRequest, TResponse>> logger)
    {
        _retryPolicy = Policy<TResponse>
            .Handle<Exception>()
            .WaitAndRetryAsync(MaxRetries,
                retryAttempt => TimeSpan.FromMilliseconds(200 * retryAttempt), // Exponential backoff
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    logger.LogWarning(
                        "Retry {RetryCount}/{MaxRetries} for {RequestName} due to: {ExceptionMessage}",
                        retryCount, MaxRetries, typeof(TRequest).Name, exception.Exception.Message);
                });
    }

    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default) => await _retryPolicy.ExecuteAsync(_ => next(), cancellationToken);
}

/// <inheritdoc />
public class RetryBehavior<TRequest> : IPipelineBehavior<TRequest> where TRequest : IRequest
{
    private const int MaxRetries = 5;

    private readonly AsyncRetryPolicy _retryPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryBehavior{TRequest}"/> class,
    /// which applies a retry policy with exponential backoff to handle transient failures during request processing.
    /// </summary>
    /// <param name="logger">
    /// The logger used to record retry attempts, including warnings on exceptions and contextual information about the request.
    /// </param>
    public RetryBehavior(ILogger<RetryBehavior<TRequest>> logger)
    {
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(MaxRetries,
                retryAttempt => TimeSpan.FromMilliseconds(200 * retryAttempt), // Exponential backoff
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    logger.LogWarning(
                        "Retry {RetryCount}/{MaxRetries} for {RequestName} due to: {ExceptionMessage}",
                        retryCount, MaxRetries, typeof(TRequest).Name, exception.Message);
                });
    }

    /// <inheritdoc />
    public async Task HandleAsync(TRequest request, Func<Task> next, CancellationToken cancellationToken = default) => await _retryPolicy.ExecuteAsync(_ => next(), cancellationToken);
}
