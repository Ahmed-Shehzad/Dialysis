using Dialysis.CQRS.Queries;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.Simulation.Contracts;
using Dialysis.Simulation.Contracts.Security;
using Dialysis.Simulation.Engine.Scenarios;

namespace Dialysis.Simulation.Engine.Features.ListScenarios;

/// <summary>Lists the registered scenario catalog.</summary>
public sealed record ListScenariosQuery : IQuery<IReadOnlyList<ScenarioSummaryDto>>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => SimulationPermissions.SessionRead;
}

/// <summary>Projects the registry into catalog summaries.</summary>
public sealed class ListScenariosQueryHandler : IQueryHandler<ListScenariosQuery, IReadOnlyList<ScenarioSummaryDto>>
{
    private readonly IScenarioRegistry _registry;

    /// <summary>Creates the handler.</summary>
    public ListScenariosQueryHandler(IScenarioRegistry registry) => _registry = registry;

    /// <inheritdoc />
    public Task<IReadOnlyList<ScenarioSummaryDto>> HandleAsync(ListScenariosQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        IReadOnlyList<ScenarioSummaryDto> result =
        [
            .. _registry.All.Select(s => new ScenarioSummaryDto(
                s.Id, s.Name, s.Description, [.. s.Steps.Select(step => step.Name)])),
        ];
        return Task.FromResult(result);
    }
}
