namespace Dialysis.Gateway.Features.Sessions.Saga;

/// <summary>
/// Single-responsibility step for session completion saga.
/// Each step has one reason to change.
/// </summary>
public interface ISessionCompletionStep
{
    /// <summary>
    /// Executes the step. Updates state and throws on failure to trigger compensation.
    /// </summary>
    Task ExecuteAsync(SessionCompletionState state, CancellationToken cancellationToken = default);
}
