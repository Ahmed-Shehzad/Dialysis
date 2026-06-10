namespace Dialysis.BuildingBlocks.Intercessor;

/// <summary>
/// Marker for a message handled by <see cref="IRequestHandler{TRequest,TResponse}"/>.
/// </summary>
/// <typeparam name="TResponse">The handler response type.</typeparam>
// TResponse is a phantom type parameter: it lets SendAsync infer the response type from
// the request object alone — the mediator marker pattern.
#pragma warning disable S2326
public interface IRequest<out TResponse>
{
}
#pragma warning restore S2326
