using Dialysis.Contracts.Events;
using Dialysis.Gateway.Features.Sessions.Saga;
using Dialysis.Persistence;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Transponder.Abstractions;

namespace Dialysis.Gateway.Features.Sessions;

public sealed class CompleteSessionCommandHandler : ICommandHandler<CompleteSessionCommand, CompleteSessionResult>
{
    private readonly DialysisDbContext _db;
    private readonly ISessionRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly IPublisher _publisher;
    private readonly ISendEndpointProvider? _sendEndpointProvider;
    private readonly SessionCompletionOptions _options;

    public CompleteSessionCommandHandler(
        DialysisDbContext db,
        ISessionRepository repository,
        ITenantContext tenantContext,
        IPublisher publisher,
        IOptions<SessionCompletionOptions> options,
        ISendEndpointProvider? sendEndpointProvider = null)
    {
        _db = db;
        _repository = repository;
        _tenantContext = tenantContext;
        _publisher = publisher;
        _options = options.Value;
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

        if (_options.UseSaga && _sendEndpointProvider is not null)
        {
            var sagaRequest = new SessionCompletionSagaRequest(
                session.Id.ToString(),
                session.PatientId.Value,
                session.TenantId.Value);
            var endpoint = await _sendEndpointProvider.GetSendEndpointAsync(new Uri("sb://dialysis/session-completion-saga"), cancellationToken);
            await endpoint.SendAsync(sagaRequest, cancellationToken);
        }
        else
        {
            await _publisher.PublishAsync(new SessionCompleted(session.Id.ToString(), session.PatientId, session.TenantId), cancellationToken);
        }

        return new CompleteSessionResult(session);
    }
}
