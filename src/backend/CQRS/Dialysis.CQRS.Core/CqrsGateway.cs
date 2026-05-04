using Dialysis.BuildingBlocks.Intercessor;
using Dialysis.CQRS.Commands;
using Dialysis.CQRS.Queries;

namespace Dialysis.CQRS;

internal sealed class CqrsGateway(IIntercessor intercessor) : ICqrsGateway
{
    public Task<TResponse> SendQueryAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>
    {
        ArgumentNullException.ThrowIfNull(query);
        return intercessor.Send<TQuery, TResponse>(query, cancellationToken);
    }

    public Task<TResponse> SendCommandAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>
    {
        ArgumentNullException.ThrowIfNull(command);
        return intercessor.Send<TCommand, TResponse>(command, cancellationToken);
    }
}
