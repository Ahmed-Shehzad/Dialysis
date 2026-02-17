using Dialysis.Contracts.Events;
using Dialysis.Domain.Aggregates;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Sessions;

public sealed class StartSessionCommandHandler : ICommandHandler<StartSessionCommand, StartSessionResult>
{
    private readonly ISessionRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly IPublisher _publisher;

    public StartSessionCommandHandler(ISessionRepository repository, ITenantContext tenantContext, IPublisher publisher)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _publisher = publisher;
    }

    public async Task<StartSessionResult> HandleAsync(StartSessionCommand request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var patientId = new PatientId(request.PatientId);
        var session = Session.Start(tenantId, patientId, request.AccessSite, request.EncounterId);
        await _repository.AddAsync(session, cancellationToken);

        await _publisher.PublishAsync(new SessionStarted(session.Id.ToString(), patientId, tenantId), cancellationToken);

        return new StartSessionResult(session);
    }
}
