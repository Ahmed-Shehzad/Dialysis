namespace Transponder.Abstractions;

/// <summary>
/// Describes the saga interaction style.
/// </summary>
/// <remarks>
/// For orchestration and choreography sagas, each step should be idempotent and compensable.
/// </remarks>
public enum SagaStyle
{
    Orchestration = 0,
    Choreography = 1
}
