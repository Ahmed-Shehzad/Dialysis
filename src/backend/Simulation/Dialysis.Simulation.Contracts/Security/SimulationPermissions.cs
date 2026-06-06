using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Simulation.Contracts.Security;

/// <summary>
/// Closed permission set for the Simulation bounded context — starting and running scenario sessions
/// and reading back their generated lineage (events, audit, record links).
/// </summary>
public static class SimulationPermissions
{
    /// <summary>Create a new simulation session for a scenario.</summary>
    public const string SessionStart = "simulation.session.start";

    /// <summary>Execute a session's scenario against the (real or in-memory) module drivers.</summary>
    public const string ScenarioRun = "simulation.scenario.run";

    /// <summary>Read a session's status, workflow state, and scenario catalog.</summary>
    public const string SessionRead = "simulation.session.read";

    /// <summary>Read a session's emitted event-store lineage.</summary>
    public const string EventsRead = "simulation.events.read";

    /// <summary>Read a session's audit trail.</summary>
    public const string AuditRead = "simulation.audit.read";

    /// <summary>Every permission in the catalog.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        SessionStart,
        ScenarioRun,
        SessionRead,
        EventsRead,
        AuditRead,
    ];
}

/// <summary>Module permission catalog consumed by the Simulation host's <c>AddModuleHost</c> registration.</summary>
public sealed class SimulationPermissionCatalog : IModulePermissionCatalog
{
    /// <inheritdoc />
    public string ModuleSlug => "simulation";

    /// <inheritdoc />
    public IReadOnlyCollection<string> All => SimulationPermissions.All;
}
