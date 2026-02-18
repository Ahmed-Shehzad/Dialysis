using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;

using Verifier;
using Verifier.Abstractions;
using Verifier.Exceptions;

namespace Intercessor.Behaviours;

/// <inheritdoc />
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationBehavior{TRequest, TResponse}"/> class,
    /// which is used to validate requests using FluentValidation before they reach the handler.
    /// </summary>
    /// <param name="validators">
    /// A collection of validators for the incoming <typeparamref name="TRequest"/>.
    /// Each validator is applied to ensure the request meets expected rules.
    /// </param>
    /// <param name="logger">
    /// The logger used to log validation information or errors.
    /// </param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators, ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        if (!_validators.Any()) return await next();

        ValidationResult[] validationResults = await Task.WhenAll(_validators.Select(x => x.ValidateAsync(request, cancellationToken)));

        var failures = validationResults.SelectMany(x => x.Errors).ToList();

        if (failures.Count == 0) return await next();

        _logger.LogError("Validation errors - {CommandType} - Command: {@Command} - Errors: {@ValidationErrors}", typeof(TRequest).Name, request, failures);
        throw new ValidationException(failures);
    }
}

/// <inheritdoc />
public class ValidationBehavior<TRequest> : IPipelineBehavior<TRequest> where TRequest : IRequest
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationBehavior{TRequest}"/> class,
    /// which is used to validate requests using FluentValidation before they reach the handler.
    /// </summary>
    /// <param name="validators">
    /// A collection of validators for the incoming <typeparamref name="TRequest"/>.
    /// Each validator is applied to ensure the request meets expected rules.
    /// </param>
    /// <param name="logger">
    /// The logger used to log validation information or errors.
    /// </param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators, ILogger<ValidationBehavior<TRequest>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleAsync(TRequest request, Func<Task> next, CancellationToken cancellationToken = default)
    {
        if (!_validators.Any())
        {
            await next();
            return;
        }

        ValidationResult[] validationResults = await Task.WhenAll(_validators.Select(x => x.ValidateAsync(request, cancellationToken)));

        var failures = validationResults.SelectMany(x => x.Errors).ToList();

        if (failures.Count == 0)
        {
            await next();
            return;
        }

        _logger.LogError("Validation errors - {CommandType} - Command: {@Command} - Errors: {@ValidationErrors}", typeof(TRequest).Name, request, failures);
        throw new ValidationException(failures);
    }
}
