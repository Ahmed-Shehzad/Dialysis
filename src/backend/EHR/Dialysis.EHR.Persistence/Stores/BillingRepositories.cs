using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;
using Dialysis.EHR.Billing.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Stores;

public sealed class ChargeRepository : IChargeRepository
{
    private readonly EhrDbContext _db;
    public ChargeRepository(EhrDbContext db) => _db = db;
    public Task<Charge?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Charges.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Charge>> ListUnbilledForPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await _db.Charges
            .Where(c => c.PatientId == patientId && c.Status == ChargeStatus.Captured)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<Charge>> ListRecentForPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default) =>
        await _db.Charges
            .AsNoTracking()
            .Where(c => c.PatientId == patientId && c.CreatedAt >= sinceUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<Charge>> ListAgedCapturedAsync(DateTime capturedBeforeUtc, int take, CancellationToken cancellationToken = default)
    {
        var bounded = Math.Clamp(take, 1, 500);
        return await _db.Charges
            .AsNoTracking()
            .Where(c => c.Status == ChargeStatus.Captured && c.CreatedAt < capturedBeforeUtc)
            .OrderBy(c => c.CreatedAt)
            .Take(bounded)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Charge>> ListAsync(ChargeStatus? status, int take, CancellationToken cancellationToken = default)
    {
        var bounded = Math.Clamp(take, 1, 500);
        var query = _db.Charges.AsQueryable();
        if (status is not null)
            query = query.Where(c => c.Status == status.Value);
        return await query
            .OrderByDescending(c => c.Id)
            .Take(bounded)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public void Add(Charge charge) => _db.Charges.Add(charge);
}

public sealed class ClaimRepository : IClaimRepository
{
    private readonly EhrDbContext _db;
    public ClaimRepository(EhrDbContext db) => _db = db;
    public Task<Claim?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Claims.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Claim?> FindByExternalControlNumberAsync(string controlNumber, CancellationToken cancellationToken = default) =>
        _db.Claims.FirstOrDefaultAsync(c => c.ExternalControlNumber == controlNumber, cancellationToken);

    public async Task<IReadOnlyList<Claim>> ListAsync(ClaimStatus? status, int take, CancellationToken cancellationToken = default)
    {
        var bounded = Math.Clamp(take, 1, 500);
        var query = _db.Claims.AsQueryable();
        if (status is not null)
            query = query.Where(c => c.Status == status.Value);
        return await query
            .OrderByDescending(c => c.SubmittedAtUtc ?? DateTime.MinValue)
            .ThenByDescending(c => c.Id)
            .Take(bounded)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public void Add(Claim claim) => _db.Claims.Add(claim);
}

public sealed class RemittanceRepository : IRemittanceRepository
{
    private readonly EhrDbContext _db;
    public RemittanceRepository(EhrDbContext db) => _db = db;
    public Task<Remittance?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Remittances.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public void Add(Remittance remittance) => _db.Remittances.Add(remittance);
}

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly EhrDbContext _db;
    public PaymentRepository(EhrDbContext db) => _db = db;
    public Task<Payment?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Payments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public void Add(Payment payment) => _db.Payments.Add(payment);
}

public sealed class PayerRepository : IPayerRepository
{
    private readonly EhrDbContext _db;
    public PayerRepository(EhrDbContext db) => _db = db;
    public Task<Payer?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Payers.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Payer?> FindByCodeAsync(string payerCode, CancellationToken cancellationToken = default) =>
        _db.Payers.FirstOrDefaultAsync(p => p.PayerCode == payerCode.ToUpperInvariant(), cancellationToken);

    public void Add(Payer payer) => _db.Payers.Add(payer);
}

public sealed class EfBillableEncounterRepository : IBillableEncounterRepository
{
    private readonly EhrDbContext _db;
    public EfBillableEncounterRepository(EhrDbContext db) => _db = db;

    public async Task UpsertAsync(Guid encounterId, Guid patientId, Guid providerId, DateTime closedAtUtc, CancellationToken cancellationToken = default)
    {
        var existing = await _db.BillableEncounters.FirstOrDefaultAsync(b => b.EncounterId == encounterId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            _db.BillableEncounters.Add(new BillableEncounter
            {
                EncounterId = encounterId,
                PatientId = patientId,
                ProviderId = providerId,
                ClosedAtUtc = closedAtUtc,
                HasCharge = false,
            });
        }
        else
        {
            existing.PatientId = patientId;
            existing.ProviderId = providerId;
            existing.ClosedAtUtc = closedAtUtc;
        }
    }

    public Task MarkHasChargeAsync(Guid encounterId, CancellationToken cancellationToken = default) =>
        _db.BillableEncounters
            .Where(b => b.EncounterId == encounterId)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.HasCharge, true), cancellationToken);

    public async Task<IReadOnlyList<BillableEncounter>> ListMissingChargesAsync(DateTime closedBeforeUtc, int take, CancellationToken cancellationToken = default)
    {
        var bounded = Math.Clamp(take, 1, 500);
        return await _db.BillableEncounters
            .AsNoTracking()
            .Where(b => !b.HasCharge && b.ClosedAtUtc < closedBeforeUtc)
            .OrderBy(b => b.ClosedAtUtc)
            .Take(bounded)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
