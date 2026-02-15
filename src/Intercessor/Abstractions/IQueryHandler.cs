namespace Intercessor.Abstractions;

/// <summary>
/// Defines a query handler for handling queries that produce a response.
/// Implement this interface to handle queries of type <typeparamref name="TQuery"/> and return a response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TQuery">
/// The type of request to handle, which must implement <see cref="IQuery{TResponse}"/>.
/// </typeparam>
/// <typeparam name="TResponse">
/// The type of response the handler will return when processing the query.
/// </typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse> where TQuery : IRequest<TResponse>;