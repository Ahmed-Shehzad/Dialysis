using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.Simulation.Contracts;
using Dialysis.Simulation.Engine.Features.GetSession;
using Dialysis.Simulation.Engine.Features.GetSessionAudit;
using Dialysis.Simulation.Engine.Features.GetSessionEvents;
using Dialysis.Simulation.Engine.Features.ListScenarios;
using Dialysis.Simulation.Engine.Features.RunScenario;
using Dialysis.Simulation.Engine.Features.StartSimulation;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Simulation.Api.Controllers.V1;

/// <summary>Starts and runs scenario sessions and reads back their generated lineage.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/simulations")]
public sealed class SimulationController : ControllerBase
{
    private readonly ICqrsGateway _gateway;

    /// <summary>Creates the controller.</summary>
    public SimulationController(ICqrsGateway gateway) => _gateway = gateway;

    /// <summary>Starts a new simulation session for a scenario.</summary>
    [HttpPost("start")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> StartAsync([FromBody] StartSimulationRequest body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var id = await _gateway
            .SendCommandAsync<StartSimulationCommand, Guid>(
                new StartSimulationCommand(body.ScenarioId, body.TenantId, body.OrganizationId, body.Seed),
                cancellationToken)
            .ConfigureAwait(false);
        return Created($"/api/v1.0/simulations/{id}", new { id });
    }

    /// <summary>Runs the session's scenario to completion or failure.</summary>
    [HttpPost("{sessionId:guid}/run-scenario")]
    [ProducesResponseType(typeof(SimulationSessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RunScenarioAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _gateway
            .SendCommandAsync<RunScenarioCommand, SimulationSessionDto>(new RunScenarioCommand(sessionId), cancellationToken)
            .ConfigureAwait(false);
        return Ok(session);
    }

    /// <summary>Gets a session's status and workflow state.</summary>
    [HttpGet("{sessionId:guid}")]
    [ProducesResponseType(typeof(SimulationSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _gateway
            .SendQueryAsync<GetSimulationSessionQuery, SimulationSessionDto?>(new GetSimulationSessionQuery(sessionId), cancellationToken)
            .ConfigureAwait(false);
        return session is null ? NotFound() : Ok(session);
    }

    /// <summary>Lists a session's event-store lineage, newest first.</summary>
    [HttpGet("{sessionId:guid}/events")]
    [ProducesResponseType(typeof(IReadOnlyList<SimulationEventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventsAsync(Guid sessionId, [FromQuery] int take = 200, CancellationToken cancellationToken = default)
    {
        var rows = await _gateway
            .SendQueryAsync<GetSessionEventsQuery, IReadOnlyList<SimulationEventDto>>(new GetSessionEventsQuery(sessionId, take), cancellationToken)
            .ConfigureAwait(false);
        return Ok(rows);
    }

    /// <summary>Lists a session's audit trail, newest first.</summary>
    [HttpGet("{sessionId:guid}/audit")]
    [ProducesResponseType(typeof(IReadOnlyList<SimulationAuditDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditAsync(Guid sessionId, [FromQuery] int take = 200, CancellationToken cancellationToken = default)
    {
        var rows = await _gateway
            .SendQueryAsync<GetSessionAuditQuery, IReadOnlyList<SimulationAuditDto>>(new GetSessionAuditQuery(sessionId, take), cancellationToken)
            .ConfigureAwait(false);
        return Ok(rows);
    }

    /// <summary>Lists the registered scenario catalog.</summary>
    [HttpGet("scenarios")]
    [ProducesResponseType(typeof(IReadOnlyList<ScenarioSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScenariosAsync(CancellationToken cancellationToken)
    {
        var rows = await _gateway
            .SendQueryAsync<ListScenariosQuery, IReadOnlyList<ScenarioSummaryDto>>(new ListScenariosQuery(), cancellationToken)
            .ConfigureAwait(false);
        return Ok(rows);
    }
}
