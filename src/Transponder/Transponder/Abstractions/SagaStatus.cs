namespace Transponder.Abstractions;

/// <summary>
/// Tracks the lifecycle status of a saga instance for orchestration or choreography.
/// </summary>
/// <remarks>
/// Each step should be idempotent and compensable.
/// </remarks>
public enum SagaStatus
{
    Running = 0,
    Completed = 1,
    Compensating = 2,
    Compensated = 3,
    Failed = 4
}
