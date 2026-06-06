using Dialysis.CQRS.Queries;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.Simulation.Contracts;
using Dialysis.Simulation.Contracts.Security;
using Dialysis.Simulation.Engine.Ports;

namespace Dialysis.Simulation.Engine.Features.GetSession;

/// <summary>Projects a session's current status and workflow state.</summary>
public sealed record GetSimulationSessionQuery(Guid SessionId) : IQuery<SimulationSessionDto?>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => SimulationPermissions.SessionRead;
}

/// <summary>Loads the session projection.</summary>
public sealed class GetSimulationSessionQueryHandler : IQueryHandler<GetSimulationSessionQuery, SimulationSessionDto?>
{
    private readonly ISimulationQueryStore _queryStore;

    /// <summary>Creates the handler.</summary>
    public GetSimulationSessionQueryHandler(ISimulationQueryStore queryStore) => _queryStore = queryStore;

    /// <inheritdoc />
    public async Task<SimulationSessionDto?> HandleAsync(GetSimulationSessionQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var session = await _queryStore.GetSessionAsync(request.SessionId, cancellationToken).ConfigureAwait(false);
        return session?.ToDto();
    }
}
