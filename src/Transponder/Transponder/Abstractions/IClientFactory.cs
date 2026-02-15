namespace Transponder.Abstractions;

/// <summary>
/// Creates request clients for request/response messaging.
/// </summary>
public interface IClientFactory
{
    /// <summary>
    /// Creates a request client for the specified request type.
    /// </summary>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <param name="timeout">Optional timeout for the request.</param>
    IRequestClient<TRequest> CreateRequestClient<TRequest>(TimeSpan? timeout = null)
        where TRequest : class, IMessage;
}
