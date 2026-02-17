using Dialysis.Persistence;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.Constants;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Gateway.Features.Adequacy;

public sealed class GetAdequacySummaryQueryHandler : IQueryHandler<GetAdequacySummaryQuery, AdequacySummaryDto>
{
    private readonly DialysisDbContext _db;
    private readonly ITenantContext _tenantContext;

    public GetAdequacySummaryQueryHandler(DialysisDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<AdequacySummaryDto> HandleAsync(GetAdequacySummaryQuery request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var pid = new PatientId(request.PatientId);

        var allObs = await _db.Observations
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId && o.PatientId == pid && AdequacyLoincCodes.AdequacyCodes.Contains(o.LoincCode.Value))
            .OrderByDescending(o => o.Effective.Value)
            .ToListAsync(cancellationToken);

        var lookup = allObs
            .GroupBy(o => o.LoincCode.Value)
            .ToDictionary(g => g.Key, g => g.First());

        return new AdequacySummaryDto(
            request.PatientId,
            GetValue(lookup, AdequacyLoincCodes.Urr),
            GetValue(lookup, AdequacyLoincCodes.KtV),
            GetValue(lookup, AdequacyLoincCodes.Hemoglobin),
            GetValue(lookup, AdequacyLoincCodes.Ferritin),
            GetValue(lookup, AdequacyLoincCodes.Tsat),
            GetValue(lookup, AdequacyLoincCodes.Pth),
            GetValue(lookup, AdequacyLoincCodes.Albumin),
            GetValue(lookup, AdequacyLoincCodes.Potassium));
    }

    private static AdequacyValueDto? GetValue(IReadOnlyDictionary<string, Domain.Aggregates.Observation> lookup, string loinc)
    {
        if (!lookup.TryGetValue(loinc, out var obs))
            return null;
        return new AdequacyValueDto(obs.NumericValue, obs.Unit?.Value, obs.Effective.Value);
    }
}
