namespace Intercessor.Abstractions;

/// <summary>
/// Marker interface to represent a request that can be handled by the mediator.
/// Implementing this interface allows an object to be handled by one or more <see cref="IRequestHandler{TRequest, TResponse}"/>.
/// </summary>
public interface IRequest;

/// <summary>
/// Represents a request that produces a response when handled.
/// Implementing this interface allows an object to be handled by one or more <see cref="IRequestHandler{TRequest, TResponse}"/>.
/// </summary>
/// <typeparam name="TResponse">
/// The type of response that the request will produce when handled.
/// </typeparam>
public interface IRequest<TResponse> : IRequest;