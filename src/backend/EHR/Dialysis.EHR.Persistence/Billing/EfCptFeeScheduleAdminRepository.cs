using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Billing;

/// <summary>
/// EF-backed <see cref="ICptFeeScheduleAdminRepository"/> over the <c>ehr_billing.CptFeeSchedule</c>
/// table. Backs the operator fee-schedule management UI; writes are committed by the caller via
/// <c>IUnitOfWork.SaveChangesAsync</c>, consistent with the other billing repositories.
/// </summary>
public sealed class EfCptFeeScheduleAdminRepository(EhrDbContext db) : ICptFeeScheduleAdminRepository
{
    public async Task<IReadOnlyList<CptFeeScheduleEntry>> ListAsync(
        string? cptCode, string? payerCode, CancellationToken cancellationToken = default)
    {
        var query = db.CptFeeSchedule.AsQueryable();
        if (!string.IsNullOrWhiteSpace(cptCode))
        {
            var code = cptCode.Trim().ToUpperInvariant();
            query = query.Where(e => e.CptCode == code);
        }
        if (!string.IsNullOrWhiteSpace(payerCode))
        {
            var payer = payerCode.Trim().ToUpperInvariant();
            query = query.Where(e => e.PayerCode == payer);
        }
        return await query
            .OrderBy(e => e.CptCode)
            .ThenBy(e => e.PayerCode)
            .ThenByDescending(e => e.EffectiveFromUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<CptFeeScheduleEntry?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.CptFeeSchedule.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public void Add(CptFeeScheduleEntry entry) => db.CptFeeSchedule.Add(entry);

    public void Remove(CptFeeScheduleEntry entry) => db.CptFeeSchedule.Remove(entry);
}
