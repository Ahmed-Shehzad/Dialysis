using Dialysis.SharedKernel.ValueObjects;

using Transponder.Abstractions;

namespace Dialysis.Gateway.Features.Sessions.Saga;

/// <summary>
/// Message to start the session completion saga. Sent after session is persisted.
/// </summary>
public sealed record SessionCompletionSagaRequest(
    string SessionId,
    string PatientId,
    string TenantId
) : IMessage;
