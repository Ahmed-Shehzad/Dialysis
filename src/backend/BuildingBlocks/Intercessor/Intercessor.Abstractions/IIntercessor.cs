namespace Dialysis.BuildingBlocks.Intercessor;

/// <summary>
/// Dispatches requests to the matching handler registered for <typeparamref name="TRequest"/>.
/// </summary>
public interface IIntercessor
{
    /// <summary>
    /// Dispatches <paramref name="request"/> through validators, pipeline behaviors, then the handler.
    /// </summary>
    Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResponse>;
}
