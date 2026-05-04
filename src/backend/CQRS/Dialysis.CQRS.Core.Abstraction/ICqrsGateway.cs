using Dialysis.CQRS.Commands;
using Dialysis.CQRS.Queries;

namespace Dialysis.CQRS;

/// <summary>
/// Application-facing entry point for dispatching CQRS messages through Intercessor.
/// </summary>
public interface ICqrsGateway
{
    Task<TResponse> SendQueryAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>;

    Task<TResponse> SendCommandAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>;
}
