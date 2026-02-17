using Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Alerts;

public sealed class ListAlertsQueryHandler : IQueryHandler<ListAlertsQuery, ListAlertsResult>
{
    private readonly IAlertRepository _repository;
    private readonly ITenantContext _tenantContext;

    public ListAlertsQueryHandler(IAlertRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<ListAlertsResult> HandleAsync(ListAlertsQuery request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PatientId))
            return new ListAlertsResult([], "patientId query parameter is required.");

        var tenantId = _tenantContext.TenantId;
        var pid = new PatientId(request.PatientId);
        var alerts = await _repository.GetByPatientAsync(tenantId, pid, request.ActiveOnly, request.Limit, request.Offset, cancellationToken);
        return new ListAlertsResult(alerts.Select(ToDto).ToList(), null);
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
