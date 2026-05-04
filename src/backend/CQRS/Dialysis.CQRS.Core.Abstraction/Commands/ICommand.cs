using Dialysis.BuildingBlocks.Intercessor;

namespace Dialysis.CQRS.Commands;

/// <summary>
/// Write-side message handled by <see cref="ICommandHandler{TCommand,TResponse}"/>.
/// </summary>
/// <typeparam name="TResponse">The handler response type.</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// Command whose handler returns <see cref="Unit"/> (no meaningful payload).
/// </summary>
public interface ICommand : ICommand<Unit>
{
}
