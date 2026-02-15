using Transponder.Persistence.Abstractions;

namespace Transponder.Abstractions;

/// <summary>
/// Optional saga state contract for tracking status.
/// </summary>
public interface ISagaStatusState : ISagaState
{
    SagaStatus Status { get; set; }
}
