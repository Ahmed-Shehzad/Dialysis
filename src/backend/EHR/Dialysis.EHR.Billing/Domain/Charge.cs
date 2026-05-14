using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.EHR.Contracts.Integration;

namespace Dialysis.EHR.Billing.Domain;

public enum ChargeStatus
{
    Captured = 1,
    OnClaim = 2,
    Paid = 3,
    Adjusted = 4,
    Written = 5,
}

/// <summary>Single billable line item (CPT-coded service) attached to an encounter.</summary>
public sealed class Charge : AggregateRoot<Guid>
{
    private readonly List<string> _diagnosisPointers = new();

    private Charge()
    {
    }

    public Charge(Guid id) : base(id)
    {
    }

    public Guid PatientId { get; private set; }

    public Guid EncounterId { get; private set; }

    public string CptCode { get; private set; } = string.Empty;

    public IReadOnlyCollection<string> DiagnosisPointerIcd10Codes => _diagnosisPointers;

    public Money BilledAmount { get; private set; } = null!;

    public ChargeStatus Status { get; private set; }

    public Guid? AssignedClaimId { get; private set; }

    public static Charge Capture(
        Guid id,
        Guid patientId,
        Guid encounterId,
        string cptCode,
        IReadOnlyList<string> diagnosisPointerIcd10Codes,
        Money billedAmount)
    {
        if (patientId == Guid.Empty) throw new ArgumentException("Patient required.", nameof(patientId));
        if (encounterId == Guid.Empty) throw new ArgumentException("Encounter required.", nameof(encounterId));
        ArgumentException.ThrowIfNullOrWhiteSpace(cptCode);
        ArgumentNullException.ThrowIfNull(billedAmount);
        if (billedAmount.Amount < 0) throw new ArgumentException("Billed amount must be non-negative.", nameof(billedAmount));
        if (diagnosisPointerIcd10Codes is null || diagnosisPointerIcd10Codes.Count == 0)
            throw new ArgumentException("At least one diagnosis pointer is required.", nameof(diagnosisPointerIcd10Codes));

        var charge = new Charge(id)
        {
            PatientId = patientId,
            EncounterId = encounterId,
            CptCode = cptCode.Trim(),
            BilledAmount = billedAmount,
            Status = ChargeStatus.Captured,
        };
        charge._diagnosisPointers.AddRange(diagnosisPointerIcd10Codes.Select(c => c.Trim()).Where(static c => !string.IsNullOrEmpty(c)));

        charge.RaiseIntegrationEvent(new ChargeCapturedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            ChargeId: id,
            PatientId: patientId,
            EncounterId: encounterId,
            CptCode: charge.CptCode,
            DiagnosisPointerIcd10Codes: charge._diagnosisPointers.ToArray(),
            BilledAmount: billedAmount.Amount,
            CurrencyCode: billedAmount.CurrencyCode));

        return charge;
    }

    public void AssignToClaim(Guid claimId)
    {
        if (Status != ChargeStatus.Captured) throw new InvalidOperationException($"Cannot assign charge in status {Status}.");
        if (claimId == Guid.Empty) throw new ArgumentException("Claim required.", nameof(claimId));
        Status = ChargeStatus.OnClaim;
        AssignedClaimId = claimId;
    }

    public void RecordRemittance() => Status = ChargeStatus.Paid;

    public void Adjust() => Status = ChargeStatus.Adjusted;
}
