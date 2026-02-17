using Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Alerts;

public sealed class GetAlertQueryHandler : IQueryHandler<GetAlertQuery, GetAlertResult>
{
    private readonly IAlertRepository _repository;
    private readonly ITenantContext _tenantContext;

    public GetAlertQueryHandler(IAlertRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<GetAlertResult> HandleAsync(GetAlertQuery request, CancellationToken cancellationToken = default)
    {
        if (!Ulid.TryParse(request.Id, out var alertUlid))
            return new GetAlertResult(null, InvalidId: true, NotFound: false);

        var tenantId = _tenantContext.TenantId;
        var alert = await _repository.GetByIdAsync(tenantId, alertUlid, cancellationToken);
        if (alert is null)
            return new GetAlertResult(null, InvalidId: false, NotFound: true);

        return new GetAlertResult(ToDto(alert), InvalidId: false, NotFound: false);
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
