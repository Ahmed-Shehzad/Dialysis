using Dialysis.BuildingBlocks.Intercessor;

namespace Dialysis.CQRS.Commands;

/// <summary>
/// Handles a <typeparamref name="TCommand"/> that implements <see cref="ICommand{TResponse}"/>.
/// </summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
}

/// <summary>
/// Handles a <typeparamref name="TCommand"/> that implements non-generic <see cref="ICommand"/>.
/// </summary>
public interface ICommandHandler<in TCommand> : ICommandHandler<TCommand, Unit>
    where TCommand : ICommand
{
}
