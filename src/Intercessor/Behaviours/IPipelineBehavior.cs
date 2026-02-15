using Intercessor.Abstractions;

namespace Intercessor.Behaviours;

/// <summary>
/// Defines a pipeline behavior that can be used to add cross-cutting concerns
/// (such as logging, validation, performance monitoring, etc.)
/// around the handling of a Intercessor request.
/// </summary>
/// <typeparam name="TRequest">The type of the request being handled.</typeparam>
/// <typeparam name="TResponse">The type of the response returned by the request handler.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the request by invoking the specified delegate, allowing additional
    /// behavior to be executed before and/or after the next delegate in the pipeline.
    /// </summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="next">The next delegate in the pipeline to invoke.</param>
    /// <param name="cancellationToken">A cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation, with a response of type <typeparamref name="TResponse"/>.</returns>
    Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a pipeline behavior that can be used to add cross-cutting concerns
/// (such as logging, validation, performance monitoring, etc.)
/// around the handling of a Intercessor request.
/// </summary>
/// <typeparam name="TRequest">The type of the request being handled.</typeparam>
public interface IPipelineBehavior<in TRequest> where TRequest : IRequest
{
    /// <summary>
    /// Handles the request by invoking the specified delegate, allowing additional
    /// behavior to be executed before and/or after the next delegate in the pipeline.
    /// </summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="next">The next delegate in the pipeline to invoke.</param>
    /// <param name="cancellationToken">A cancellation token for the operation.</param>
    Task HandleAsync(TRequest request, Func<Task> next, CancellationToken cancellationToken = default);
}