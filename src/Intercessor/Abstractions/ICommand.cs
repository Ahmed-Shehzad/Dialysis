namespace Intercessor.Abstractions;

/// <summary>
/// Defines a command interface for operations that modify state or perform actions.
/// </summary>
public interface ICommand : IRequest;

/// <summary>
/// Defines a generic command interface for operations that modify state or perform actions with a specific response type <see cref="IRequest{TResponse}"/>.
/// </summary>
/// <typeparam name="TResponse">The type of the response returned by the command.</typeparam>
public interface ICommand<TResponse> : IRequest<TResponse>, ICommand;