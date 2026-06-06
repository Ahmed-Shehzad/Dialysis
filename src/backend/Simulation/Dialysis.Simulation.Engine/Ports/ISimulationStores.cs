using Dialysis.Simulation.Engine.Domain;

namespace Dialysis.Simulation.Engine.Ports;

/// <summary>Persistence port for the <see cref="SimulationSession"/> aggregate.</summary>
public interface ISimulationSessionRepository
{
    /// <summary>Tracks a new session (and its journey) for insertion.</summary>
    void Add(SimulationSession session);

    /// <summary>Loads a session with its patient journey, or <c>null</c>.</summary>
    Task<SimulationSession?> FindAsync(Guid id, CancellationToken cancellationToken);
}

/// <summary>Append-only writes the engine makes as it drives a scenario (same unit of work as the session).</summary>
public interface ISimulationWriteStore
{
    /// <summary>Tracks an event-store row for insertion.</summary>
    void AppendEvent(SimulationEventRecord record);

    /// <summary>Tracks an audit entry for insertion.</summary>
    void AppendAudit(SimulationAuditEntry entry);

    /// <summary>Tracks a record link for insertion.</summary>
    void AppendLink(SessionRecordLink link);
}

/// <summary>Read-side projections for the query handlers.</summary>
public interface ISimulationQueryStore
{
    /// <summary>Loads a session (with journey) for projection, or <c>null</c>.</summary>
    Task<SimulationSession?> GetSessionAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Lists a session's event-store rows, newest first.</summary>
    Task<IReadOnlyList<SimulationEventRecord>> ListEventsAsync(Guid sessionId, int take, CancellationToken cancellationToken);

    /// <summary>Lists a session's audit entries, newest first.</summary>
    Task<IReadOnlyList<SimulationAuditEntry>> ListAuditAsync(Guid sessionId, int take, CancellationToken cancellationToken);
}
