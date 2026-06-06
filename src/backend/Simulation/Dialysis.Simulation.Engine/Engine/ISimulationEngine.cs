namespace Dialysis.Simulation.Engine.Engine;

/// <summary>Executes a session's scenario, driving the modules and recording the full lineage.</summary>
public interface ISimulationEngine
{
    /// <summary>
    /// Runs the scenario for the given session to completion or failure. Idempotent for sessions that
    /// have already reached a terminal state.
    /// </summary>
    Task RunAsync(Guid sessionId, CancellationToken cancellationToken);
}
