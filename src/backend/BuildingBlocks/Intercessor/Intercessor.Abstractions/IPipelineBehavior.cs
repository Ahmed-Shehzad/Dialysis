namespace Dialysis.BuildingBlocks.Intercessor;

/// <summary>
/// Delegate passed to a pipeline behavior to invoke the rest of the chain (including the handler).
/// </summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Cross-cutting behavior around a request. Behaviors run after validation and in registration order
/// (first registered is the outermost wrapper).
/// </summary>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
