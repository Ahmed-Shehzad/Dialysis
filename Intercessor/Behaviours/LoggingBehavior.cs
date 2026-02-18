using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;

namespace Intercessor.Behaviours;

/// <inheritdoc />
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingBehavior{TRequest, TResponse}"/> class,
    /// which logs information about the request and its handling for diagnostic and monitoring purposes.
    /// </summary>
    /// <param name="logger">
    /// The logger used to record request execution details, such as start, end, and exceptions.
    /// </param>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Handling {Name}", typeof(TRequest).Name);

        TResponse response = await next();

        _logger.LogTrace("Handled {Name}", typeof(TRequest).Name);
        return response;
    }
}

/// <inheritdoc />
public class LoggingBehavior<TRequest> : IPipelineBehavior<TRequest> where TRequest : IRequest
{
    private readonly ILogger<LoggingBehavior<TRequest>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingBehavior{TRequest}"/> class,
    /// which logs information about the request and its handling for diagnostic and monitoring purposes.
    /// </summary>
    /// <param name="logger">
    /// The logger used to record request execution details, such as start, end, and exceptions.
    /// </param>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest>> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleAsync(TRequest request, Func<Task> next, CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Handling {Name}", typeof(TRequest).Name);

        await next();

        _logger.LogTrace("Handled {Name}", typeof(TRequest).Name);
    }
}
