using Dialysis.BuildingBlocks.Intercessor;

namespace Dialysis.CQRS.Queries;

/// <summary>
/// Handles a <typeparamref name="TQuery"/> that implements <see cref="IQuery{TResponse}"/>.
/// </summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}
