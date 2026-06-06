using Dialysis.CQRS.Queries;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.Simulation.Contracts;
using Dialysis.Simulation.Contracts.Security;
using Dialysis.Simulation.Engine.Ports;

namespace Dialysis.Simulation.Engine.Features.GetSessionEvents;

/// <summary>Lists a session's event-store rows (the generated lineage).</summary>
public sealed record GetSessionEventsQuery(Guid SessionId, int Take = 200)
    : IQuery<IReadOnlyList<SimulationEventDto>>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => SimulationPermissions.EventsRead;
}

/// <summary>Loads the event-store projection.</summary>
public sealed class GetSessionEventsQueryHandler : IQueryHandler<GetSessionEventsQuery, IReadOnlyList<SimulationEventDto>>
{
    private readonly ISimulationQueryStore _queryStore;

    /// <summary>Creates the handler.</summary>
    public GetSessionEventsQueryHandler(ISimulationQueryStore queryStore) => _queryStore = queryStore;

    /// <inheritdoc />
    public async Task<IReadOnlyList<SimulationEventDto>> HandleAsync(GetSessionEventsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var take = Math.Clamp(request.Take, 1, 1000);
        var rows = await _queryStore.ListEventsAsync(request.SessionId, take, cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => r.ToDto())];
    }
}
