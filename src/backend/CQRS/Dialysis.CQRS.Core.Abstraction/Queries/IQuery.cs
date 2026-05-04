using Dialysis.BuildingBlocks.Intercessor;

namespace Dialysis.CQRS.Queries;

/// <summary>
/// Read-side message handled by <see cref="IQueryHandler{TQuery,TResponse}"/>.
/// </summary>
/// <typeparam name="TResponse">The handler response type.</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
