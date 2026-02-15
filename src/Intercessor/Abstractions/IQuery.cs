namespace Intercessor.Abstractions;

/// <summary>
/// Marker interface to represent a query that produces a response when handled.
/// </summary>
public interface IQuery : IRequest;

/// <summary>
/// Represents a query that produces a response when handled by <see cref="IRequestHandler{TRequest, TResponse}"/>.
/// </summary>
/// <typeparam name="TResponse">
/// The type of response that the request will produce when handled.
/// </typeparam>
public interface IQuery<TResponse> : IRequest<TResponse>, IQuery;
