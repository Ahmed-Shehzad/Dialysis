using Dialysis.Gateway.Features.Sessions.Saga;
using Dialysis.Persistence;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;

using Transponder.Abstractions;

namespace Dialysis.Gateway.Features.Sessions;

/// <summary>
/// Completes session and enqueues to Transponder saga for EHR push and audit.
/// Session completion uses saga orchestration only (no choreography fallback).
/// Requires Transponder (EventExport with ASB) to be configured.
/// </summary>
public sealed class CompleteSessionCommandHandler : ICommandHandler<CompleteSessionCommand, CompleteSessionResult>
{
    private readonly DialysisDbContext _db;
    private readonly ISessionRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ISendEndpointProvider _sendEndpointProvider;

    public CompleteSessionCommandHandler(
        DialysisDbContext db,
        ISessionRepository repository,
        ITenantContext tenantContext,
        ISendEndpointProvider sendEndpointProvider)
    {
        _db = db;
        _repository = repository;
        _tenantContext = tenantContext;
        _sendEndpointProvider = sendEndpointProvider;
    }

    public async Task<CompleteSessionResult> HandleAsync(CompleteSessionCommand request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var sessionId = new SessionId(request.SessionId);
        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id.ToString() == sessionId.Value, cancellationToken);
        if (session is null)
            return new CompleteSessionResult(null);

        session.Complete(request.UfRemovedKg);
        await _repository.SaveChangesAsync(cancellationToken);

        var sagaRequest = new SessionCompletionSagaRequest(
            session.Id.ToString(),
            session.PatientId.Value,
            session.TenantId.Value);
        var endpoint = await _sendEndpointProvider.GetSendEndpointAsync(new Uri("sb://dialysis/session-completion-saga"), cancellationToken);
        await endpoint.SendAsync(sagaRequest, cancellationToken);

        return new CompleteSessionResult(session);
    }
}
