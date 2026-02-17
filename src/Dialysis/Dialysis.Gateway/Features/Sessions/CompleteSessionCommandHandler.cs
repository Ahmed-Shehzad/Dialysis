using Dialysis.Contracts.Events;
using Dialysis.Persistence;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Gateway.Features.Sessions;

public sealed class CompleteSessionCommandHandler : ICommandHandler<CompleteSessionCommand, CompleteSessionResult>
{
    private readonly DialysisDbContext _db;
    private readonly ISessionRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly IPublisher _publisher;

    public CompleteSessionCommandHandler(
        DialysisDbContext db,
        ISessionRepository repository,
        ITenantContext tenantContext,
        IPublisher publisher)
    {
        _db = db;
        _repository = repository;
        _tenantContext = tenantContext;
        _publisher = publisher;
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

        await _publisher.PublishAsync(new SessionCompleted(session.Id.ToString(), session.PatientId, session.TenantId), cancellationToken);

        return new CompleteSessionResult(session);
    }
}
