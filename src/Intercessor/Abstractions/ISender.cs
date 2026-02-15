namespace Intercessor.Abstractions;

/// <summary>
/// Defines a sender that is responsible for sending requests (Commands/Queries) to the appropriate handlers.
/// Implementing this interface allows sending requests of type <see cref="IRequest{TResponse}"/> and receiving responses.
/// </summary>
public interface ISender
{
    /// <summary>
    /// Sends a request (Command or Query) to the appropriate handler asynchronously to get a response.
    /// </summary>
    /// <typeparam name="TResponse">
    /// The type of response the request will produce when handled.
    /// </typeparam>
    /// <param name="request">The request to send, which must implement <see cref="IRequest{TResponse}"/>.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, with the <typeparamref name="TResponse"/> result.</returns>
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a request (Command) to the appropriate handler asynchronously.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SendAsync(IRequest request, CancellationToken cancellationToken = default);
}
