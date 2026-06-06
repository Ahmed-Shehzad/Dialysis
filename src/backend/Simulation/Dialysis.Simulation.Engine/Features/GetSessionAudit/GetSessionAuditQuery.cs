using Dialysis.CQRS.Queries;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.Simulation.Contracts;
using Dialysis.Simulation.Contracts.Security;
using Dialysis.Simulation.Engine.Ports;

namespace Dialysis.Simulation.Engine.Features.GetSessionAudit;

/// <summary>Lists a session's audit trail.</summary>
public sealed record GetSessionAuditQuery(Guid SessionId, int Take = 200)
    : IQuery<IReadOnlyList<SimulationAuditDto>>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => SimulationPermissions.AuditRead;
}

/// <summary>Loads the audit projection.</summary>
public sealed class GetSessionAuditQueryHandler : IQueryHandler<GetSessionAuditQuery, IReadOnlyList<SimulationAuditDto>>
{
    private readonly ISimulationQueryStore _queryStore;

    /// <summary>Creates the handler.</summary>
    public GetSessionAuditQueryHandler(ISimulationQueryStore queryStore) => _queryStore = queryStore;

    /// <inheritdoc />
    public async Task<IReadOnlyList<SimulationAuditDto>> HandleAsync(GetSessionAuditQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var take = Math.Clamp(request.Take, 1, 1000);
        var rows = await _queryStore.ListAuditAsync(request.SessionId, take, cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => r.ToDto())];
    }
}
