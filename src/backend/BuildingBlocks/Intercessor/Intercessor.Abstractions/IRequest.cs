namespace Dialysis.BuildingBlocks.Intercessor;

/// <summary>
/// Marker for a message handled by <see cref="IRequestHandler{TRequest,TResponse}"/>.
/// </summary>
/// <typeparam name="TResponse">The handler response type.</typeparam>
public interface IRequest<out TResponse>
{
}
