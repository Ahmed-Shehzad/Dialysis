using Transponder.Abstractions;
using Transponder.Persistence.Abstractions;

namespace Dialysis.Gateway.Features.Sessions.Saga;

/// <summary>
/// Persistent state for the session completion saga.
/// </summary>
public sealed class SessionCompletionState : ISagaState, ISagaStatusState
{
    public Ulid CorrelationId { get; set; }
    public Ulid? ConversationId { get; set; }
    public int Version { get; set; }
    public SagaStatus Status { get; set; }

    public string SessionId { get; set; } = "";
    public string PatientId { get; set; } = "";
    public string TenantId { get; set; } = "";
    public bool EhrPushSucceeded { get; set; }
    public bool AuditRecorded { get; set; }
    public bool EventExported { get; set; }
}
