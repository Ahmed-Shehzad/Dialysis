using BuildingBlocks.Tenancy;

using Dialysis.Treatment.Application.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Treatment.Application.Features.GetTreatmentSessions;

public sealed class GetTreatmentSessionsQueryHandler : IQueryHandler<GetTreatmentSessionsQuery, GetTreatmentSessionsResponse>
{
    private readonly ITreatmentReadStore _readStore;
    private readonly ITenantContext _tenant;

    public GetTreatmentSessionsQueryHandler(ITreatmentReadStore readStore, ITenantContext tenant)
    {
        _readStore = readStore;
        _tenant = tenant;
    }

    public async Task<GetTreatmentSessionsResponse> HandleAsync(GetTreatmentSessionsQuery request, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TreatmentSessionReadDto> sessions = request.Subject is not null || request.DateFrom.HasValue || request.DateTo.HasValue
            ? await _readStore.SearchAsync(_tenant.TenantId, request.Subject?.Value, request.DateFrom, request.DateTo, request.Limit, cancellationToken)
            : await _readStore.GetAllForTenantAsync(_tenant.TenantId, request.Limit, cancellationToken);

        var summaries = sessions.Select(s => new TreatmentSessionSummary(
            s.SessionId,
            s.PatientMrn,
            s.DeviceId,
            s.Status,
            s.StartedAt,
            s.EndedAt,
            s.Observations.ToList())).ToList();
        return new GetTreatmentSessionsResponse(summaries);
    }
}
