using BuildingBlocks.Abstractions;

using Dialysis.Prescription.Application.Domain.Events;

using Microsoft.Extensions.Logging;

namespace Dialysis.Prescription.Application.Features.PrescriptionReceived;

internal sealed class PrescriptionReceivedEventHandler : IDomainEventHandler<PrescriptionReceivedEvent>
{
    private readonly ILogger<PrescriptionReceivedEventHandler> _logger;
    private readonly IAuditRecorder _audit;

    public PrescriptionReceivedEventHandler(ILogger<PrescriptionReceivedEventHandler> logger, IAuditRecorder audit)
    {
        _logger = logger;
        _audit = audit;
    }

    public async Task HandleAsync(PrescriptionReceivedEvent notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DomainEvent: PrescriptionReceived PrescriptionId={PrescriptionId} OrderId={OrderId} PatientMrn={PatientMrn} TenantId={TenantId}",
            notification.PrescriptionId,
            notification.OrderId.Value,
            notification.PatientMrn.Value,
            notification.TenantId);

        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Create,
            "Prescription",
            notification.PrescriptionId.ToString(),
            null,
            AuditOutcome.Success,
            "Prescription received (RSP^K22 domain event)",
            null),
            cancellationToken);
    }
}
