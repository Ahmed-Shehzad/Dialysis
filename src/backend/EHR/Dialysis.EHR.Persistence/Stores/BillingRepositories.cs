using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Stores;

public sealed class ChargeRepository(EhrDbContext db) : IChargeRepository
{
    public Task<Charge?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Charges.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Charge>> ListUnbilledForPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await db.Charges
            .Where(c => c.PatientId == patientId && c.Status == ChargeStatus.Captured)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<Charge>> ListAsync(ChargeStatus? status, int take, CancellationToken cancellationToken = default)
    {
        var bounded = Math.Clamp(take, 1, 500);
        var query = db.Charges.AsQueryable();
        if (status is not null)
            query = query.Where(c => c.Status == status.Value);
        return await query
            .OrderByDescending(c => c.Id)
            .Take(bounded)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public void Add(Charge charge) => db.Charges.Add(charge);
}

public sealed class ClaimRepository(EhrDbContext db) : IClaimRepository
{
    public Task<Claim?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Claims.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Claim?> FindByExternalControlNumberAsync(string controlNumber, CancellationToken cancellationToken = default) =>
        db.Claims.FirstOrDefaultAsync(c => c.ExternalControlNumber == controlNumber, cancellationToken);

    public async Task<IReadOnlyList<Claim>> ListAsync(ClaimStatus? status, int take, CancellationToken cancellationToken = default)
    {
        var bounded = Math.Clamp(take, 1, 500);
        var query = db.Claims.AsQueryable();
        if (status is not null)
            query = query.Where(c => c.Status == status.Value);
        return await query
            .OrderByDescending(c => c.SubmittedAtUtc ?? DateTime.MinValue)
            .ThenByDescending(c => c.Id)
            .Take(bounded)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public void Add(Claim claim) => db.Claims.Add(claim);
}

public sealed class RemittanceRepository(EhrDbContext db) : IRemittanceRepository
{
    public Task<Remittance?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Remittances.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public void Add(Remittance remittance) => db.Remittances.Add(remittance);
}

public sealed class PaymentRepository(EhrDbContext db) : IPaymentRepository
{
    public Task<Payment?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Payments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public void Add(Payment payment) => db.Payments.Add(payment);
}

public sealed class PayerRepository(EhrDbContext db) : IPayerRepository
{
    public Task<Payer?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Payers.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Payer?> FindByCodeAsync(string payerCode, CancellationToken cancellationToken = default) =>
        db.Payers.FirstOrDefaultAsync(p => p.PayerCode == payerCode.ToUpperInvariant(), cancellationToken);

    public void Add(Payer payer) => db.Payers.Add(payer);
}
