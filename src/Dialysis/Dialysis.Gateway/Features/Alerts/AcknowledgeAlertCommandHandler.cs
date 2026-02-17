using Dialysis.Domain.Entities;
using Dialysis.Gateway.Features.Audit;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Alerts;

public sealed class AcknowledgeAlertCommandHandler : ICommandHandler<AcknowledgeAlertCommand, AcknowledgeAlertResult>
{
    private readonly IAlertRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ISender _sender;

    public AcknowledgeAlertCommandHandler(IAlertRepository repository, ITenantContext tenantContext, ISender sender)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _sender = sender;
    }

    public async Task<AcknowledgeAlertResult> HandleAsync(AcknowledgeAlertCommand request, CancellationToken cancellationToken = default)
    {
        if (!Ulid.TryParse(request.Id, out var alertUlid))
            return new AcknowledgeAlertResult(null, NotFound: false, InvalidId: true);

        var tenantId = _tenantContext.TenantId;
        var alert = await _repository.GetByIdAsync(tenantId, alertUlid, cancellationToken);
        if (alert is null)
            return new AcknowledgeAlertResult(null, NotFound: true, InvalidId: false);

        alert.Acknowledge(request.AcknowledgedBy);
        await _repository.SaveChangesAsync(cancellationToken);

        await _sender.SendAsync(new RecordAuditCommand(
            Action: "AlertAcknowledged",
            ResourceType: "Alert",
            Actor: request.AcknowledgedBy ?? "api",
            ResourceId: request.Id,
            PatientId: alert.PatientId.Value,
            Details: null),
            cancellationToken);

        return new AcknowledgeAlertResult(ToDto(alert), NotFound: false, InvalidId: false);
    }

    private static AlertDto ToDto(Alert a) => new(
        a.Id.ToString(),
        a.PatientId.Value,
        a.ObservationId?.ToString(),
        a.Severity,
        a.Message,
        a.Status.ToString(),
        a.CreatedAtUtc,
        a.AcknowledgedAtUtc,
        a.AcknowledgedBy);
}
