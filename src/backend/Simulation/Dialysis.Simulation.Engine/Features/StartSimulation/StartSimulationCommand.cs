using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.Simulation.Contracts.Security;
using Dialysis.Simulation.Engine.Domain;
using Dialysis.Simulation.Engine.Generation;
using Dialysis.Simulation.Engine.Ports;

namespace Dialysis.Simulation.Engine.Features.StartSimulation;

/// <summary>
/// Creates a new simulation session for a scenario, generating the deterministic patient journey from
/// <paramref name="Seed"/> + <paramref name="ScenarioId"/> + <paramref name="TenantId"/>.
/// </summary>
public sealed record StartSimulationCommand(
    string ScenarioId,
    string TenantId,
    string OrganizationId,
    long Seed) : ICommand<Guid>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => SimulationPermissions.SessionStart;
}

/// <summary>Generates the journey and persists the new session.</summary>
public sealed class StartSimulationCommandHandler : ICommandHandler<StartSimulationCommand, Guid>
{
    private readonly IJourneyGenerator _generator;
    private readonly ISimulationSessionRepository _sessions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the handler.</summary>
    public StartSimulationCommandHandler(
        IJourneyGenerator generator,
        ISimulationSessionRepository sessions,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _generator = generator;
        _sessions = sessions;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<Guid> HandleAsync(StartSimulationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var journey = _generator.Generate(request.ScenarioId, request.TenantId, request.Seed);
        var session = SimulationSession.Start(
            request.ScenarioId,
            request.TenantId,
            request.OrganizationId,
            request.Seed,
            Guid.CreateVersion7(),
            journey.MedicalRecordNumber,
            journey.FamilyName,
            journey.GivenName,
            journey.DateOfBirth,
            journey.SexAtBirthCode,
            _timeProvider.GetUtcNow().UtcDateTime);

        _sessions.Add(session);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return session.Id;
    }
}
