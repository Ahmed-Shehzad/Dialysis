using Dialysis.BuildingBlocks.Intercessor;
using Dialysis.CQRS.Commands;
using Dialysis.CQRS.Queries;

namespace Dialysis.CQRS;

internal sealed class CqrsGateway : ICqrsGateway
{
    private readonly IIntercessor _intercessor;
    public CqrsGateway(IIntercessor intercessor) => _intercessor = intercessor;
    public Task<TResponse> SendQueryAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>
    {
        ArgumentNullException.ThrowIfNull(query);
        return _intercessor.SendAsync<TQuery, TResponse>(query, cancellationToken);
    }

    public Task<TResponse> SendCommandAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>
    {
        ArgumentNullException.ThrowIfNull(command);
        return _intercessor.SendAsync<TCommand, TResponse>(command, cancellationToken);
    }
}
