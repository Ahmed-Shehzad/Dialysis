using Dialysis.CQRS;
using Dialysis.Module.Hosting.Pipeline;
using Dialysis.Simulation.Contracts;
using Dialysis.Simulation.Engine;
using Dialysis.Simulation.Engine.Features.GetSession;
using Dialysis.Simulation.Engine.Features.GetSessionAudit;
using Dialysis.Simulation.Engine.Features.GetSessionEvents;
using Dialysis.Simulation.Engine.Features.ListScenarios;
using Dialysis.Simulation.Engine.Features.RunScenario;
using Dialysis.Simulation.Engine.Features.StartSimulation;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.Simulation.Composition;

/// <summary>
/// Wires the shared CQRS library into Simulation: handler/validator scanning from the engine plus the
/// authorization pipeline behavior for each permissioned command/query.
/// </summary>
public static class SimulationCqrsServiceCollectionExtensions
{
    /// <summary>Registers Simulation CQRS handlers + authorization behaviors.</summary>
    public static IServiceCollection AddSimulationCqrs(this IServiceCollection services) =>
        services.AddCqrs(cqrs =>
        {
            cqrs.AddFromAssembliesOf(typeof(SimulationEngineMarker));

            cqrs.AddCommandBehavior<StartSimulationCommand, Guid,
                AuthorizationPipelineBehavior<StartSimulationCommand, Guid>>();
            cqrs.AddCommandBehavior<RunScenarioCommand, SimulationSessionDto,
                AuthorizationPipelineBehavior<RunScenarioCommand, SimulationSessionDto>>();
            cqrs.AddQueryBehavior<GetSimulationSessionQuery, SimulationSessionDto?,
                AuthorizationPipelineBehavior<GetSimulationSessionQuery, SimulationSessionDto?>>();
            cqrs.AddQueryBehavior<GetSessionEventsQuery, IReadOnlyList<SimulationEventDto>,
                AuthorizationPipelineBehavior<GetSessionEventsQuery, IReadOnlyList<SimulationEventDto>>>();
            cqrs.AddQueryBehavior<GetSessionAuditQuery, IReadOnlyList<SimulationAuditDto>,
                AuthorizationPipelineBehavior<GetSessionAuditQuery, IReadOnlyList<SimulationAuditDto>>>();
            cqrs.AddQueryBehavior<ListScenariosQuery, IReadOnlyList<ScenarioSummaryDto>,
                AuthorizationPipelineBehavior<ListScenariosQuery, IReadOnlyList<ScenarioSummaryDto>>>();
        });
}
