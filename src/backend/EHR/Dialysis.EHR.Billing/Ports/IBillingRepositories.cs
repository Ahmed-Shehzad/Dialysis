using Dialysis.EHR.Billing.Domain;

namespace Dialysis.EHR.Billing.Ports;

public interface IChargeRepository
{
    Task<Charge?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Charge>> ListUnbilledForPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Charge>> ListAsync(ChargeStatus? status, int take, CancellationToken cancellationToken = default);
    void Add(Charge charge);
}

public interface IClaimRepository
{
    Task<Claim?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Claim?> FindByExternalControlNumberAsync(string controlNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Claim>> ListAsync(ClaimStatus? status, int take, CancellationToken cancellationToken = default);
    void Add(Claim claim);
}

public interface IRemittanceRepository
{
    Task<Remittance?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(Remittance remittance);
}

public interface IPaymentRepository
{
    Task<Payment?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(Payment payment);
}

public interface IPayerRepository
{
    Task<Payer?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Payer?> FindByCodeAsync(string payerCode, CancellationToken cancellationToken = default);
    void Add(Payer payer);
}

/// <summary>
/// Admin CRUD over the per-payer / per-CPT <see cref="CptFeeScheduleEntry"/> table. Distinct
/// from <c>ICptFeeSchedule</c> (which is read-only resolution on the charge path): this port
/// backs the operator fee-schedule management UI. Write operations are persisted by the caller
/// via <c>IUnitOfWork.SaveChangesAsync</c>, matching the other billing repositories.
/// </summary>
public interface ICptFeeScheduleAdminRepository
{
    Task<IReadOnlyList<CptFeeScheduleEntry>> ListAsync(
        string? cptCode, string? payerCode, CancellationToken cancellationToken = default);
    Task<CptFeeScheduleEntry?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(CptFeeScheduleEntry entry);
    void Remove(CptFeeScheduleEntry entry);
}

/// <summary>
/// Maps (sessionId, cptCode) → existing chargeId so the
/// <c>DialysisSessionChargeReadyConsumer</c> can stay idempotent under at-least-once
/// delivery. The mapping is owned by the persistence layer (one row per session +
/// CPT combination) so the consumer never has to scan the full Charge table.
/// </summary>
public interface IChargeIdempotencyStore
{
    Task<Guid?> FindChargeIdAsync(Guid sessionId, string cptCode, CancellationToken cancellationToken);
    Task RegisterAsync(Guid sessionId, string cptCode, Guid chargeId, CancellationToken cancellationToken);
}
