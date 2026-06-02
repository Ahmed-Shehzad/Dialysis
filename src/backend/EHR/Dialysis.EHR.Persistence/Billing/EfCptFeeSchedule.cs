using Dialysis.EHR.Billing.Consumers;
using Dialysis.EHR.Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Billing;

/// <summary>
/// EF-backed <see cref="ICptFeeSchedule"/>. Resolves the most-specific matching row from
/// the <see cref="CptFeeScheduleEntry"/> table: exact-payer wins over wildcard; among
/// matching rows, the latest <c>EffectiveFromUtc</c> still &lt;= today wins.
///
/// Production deployments populate this table once at onboarding plus on every payer
/// rate change; the admin UI lives under /admin/billing/fee-schedule (UI lands in PR 8).
/// </summary>
public sealed class EfCptFeeSchedule : ICptFeeSchedule
{
    private readonly DbContext _dbContext;
    private readonly TimeProvider _clock;

    public EfCptFeeSchedule(DbContext dbContext, TimeProvider clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<Money> LookupAsync(string cptCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cptCode);
        var today = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);

        // Pull every row for this CPT (typically < 10 rows per code across all payers and
        // effective dates) and pick the most-specific in memory — cheaper than a complex
        // SQL with conditional ranks and works the same regardless of provider.
        var candidates = await _dbContext.Set<CptFeeScheduleEntry>()
            .Where(e => e.CptCode == cptCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var match = candidates
            .Where(e => e.CoversInstant(today))
            .OrderByDescending(e => e.PayerCode != "*")
            .ThenByDescending(e => e.EffectiveFromUtc)
            .FirstOrDefault();

        return match?.Amount ?? throw new InvalidOperationException(
            $"No CPT fee-schedule row matches code '{cptCode}' for date {today:O}. " +
            "Seed the table or fall back to the ConfigurableCptFeeSchedule for dev.");
    }
}
