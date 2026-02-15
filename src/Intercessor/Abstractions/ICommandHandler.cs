namespace Intercessor.Abstractions;

/// <summary>
/// Defines a command handler for handling commands that produce a response.
/// Implement this interface to handle commands of type <typeparamref name="TCommand"/> and return a response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TCommand">
/// The type of request to handle, which must implement <see cref="IRequest{TResponse}"/>.
/// </typeparam>
/// <typeparam name="TResponse">
/// The type of response the handler will return when processing the command.
/// </typeparam>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse> where TCommand : IRequest<TResponse>;

/// <summary>
/// Defines a command handler for handling commands that does not produce a response.
/// </summary>
/// <typeparam name="TCommand"></typeparam>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand> where TCommand : IRequest;