using Dialysis.CQRS.Commands;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.Simulation.Contracts;
using Dialysis.Simulation.Contracts.Security;
using Dialysis.Simulation.Engine.Engine;
using Dialysis.Simulation.Engine.Ports;

namespace Dialysis.Simulation.Engine.Features.RunScenario;

/// <summary>Runs a session's scenario to completion or failure and returns the resulting session state.</summary>
public sealed record RunScenarioCommand(Guid SessionId) : ICommand<SimulationSessionDto>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => SimulationPermissions.ScenarioRun;
}

/// <summary>Invokes the engine and returns the session projection.</summary>
public sealed class RunScenarioCommandHandler : ICommandHandler<RunScenarioCommand, SimulationSessionDto>
{
    private readonly ISimulationEngine _engine;
    private readonly ISimulationQueryStore _queryStore;

    /// <summary>Creates the handler.</summary>
    public RunScenarioCommandHandler(ISimulationEngine engine, ISimulationQueryStore queryStore)
    {
        _engine = engine;
        _queryStore = queryStore;
    }

    /// <inheritdoc />
    public async Task<SimulationSessionDto> HandleAsync(RunScenarioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _engine.RunAsync(request.SessionId, cancellationToken).ConfigureAwait(false);

        var session = await _queryStore.GetSessionAsync(request.SessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Simulation session {request.SessionId} was not found.");
        return session.ToDto();
    }
}
