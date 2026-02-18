namespace Transponder.Abstractions;

/// <summary>
/// Provides request/response messaging for a given request type.
/// </summary>
/// <typeparam name="TRequest">The request message type.</typeparam>
public interface IRequestClient<in TRequest> where TRequest : class, IMessage
{
    /// <summary>
    /// Sends a request message and awaits a response.
    /// </summary>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="request">The request message.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task<TResponse> GetResponseAsync<TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TResponse : class, IMessage;
}
