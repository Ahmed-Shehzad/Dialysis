using Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Audit;

public sealed class RecordAuditCommandHandler : ICommandHandler<RecordAuditCommand, RecordAuditResult>
{
    private readonly IAuditRepository _repository;
    private readonly ITenantContext _tenantContext;

    public RecordAuditCommandHandler(IAuditRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<RecordAuditResult> HandleAsync(RecordAuditCommand request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Action) || string.IsNullOrWhiteSpace(request.ResourceType))
            return new RecordAuditResult(null, "Action and ResourceType are required.");

        var tenantId = _tenantContext.TenantId;
        var evt = AuditEvent.Create(
            tenantId,
            request.Actor ?? "api",
            request.Action,
            request.ResourceType,
            request.ResourceId,
            request.PatientId,
            request.Details);

        await _repository.AddAsync(evt, cancellationToken);

        return new RecordAuditResult(ToDto(evt), null);
    }

    private static AuditEventDto ToDto(AuditEvent e) => new(
        e.Id.ToString(),
        e.Actor,
        e.Action,
        e.ResourceType,
        e.ResourceId,
        e.PatientId,
        e.CreatedAtUtc,
        e.Details);
}
