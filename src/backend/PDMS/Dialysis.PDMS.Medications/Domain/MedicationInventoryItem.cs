using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.PDMS.Medications.Contracts;

namespace Dialysis.PDMS.Medications.Domain;

/// <summary>
/// One inventory row per <c>(MedicationCoding, LotNumber)</c>. Operators receive, deduct on
/// administration (via the OnMedicationAdministered consumer), and adjust on physical count
/// reconciliation. Falling below the threshold raises
/// <see cref="MedicationInventoryLowIntegrationEvent"/>.
/// </summary>
public sealed class MedicationInventoryItem : AggregateRoot<Guid>
{
    private MedicationInventoryItem() { }

    public MedicationInventoryItem(
        Guid id,
        MedicationCoding medication,
        string lotNumber,
        DateTime expiryUtc,
        int initialOnHand,
        int threshold) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lotNumber);
        if (initialOnHand < 0)
            throw new ArgumentOutOfRangeException(nameof(initialOnHand), "Initial on-hand must be ≥ 0.");
        if (threshold < 0)
            throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be ≥ 0.");
        Medication = medication;
        LotNumber = lotNumber;
        ExpiryUtc = expiryUtc;
        OnHandUnits = initialOnHand;
        Threshold = threshold;
    }

    public MedicationCoding Medication { get; private set; } = null!;
    public string LotNumber { get; private set; } = null!;
    public DateTime ExpiryUtc { get; private set; }
    public int OnHandUnits { get; private set; }
    public int Threshold { get; private set; }

    /// <summary>Operator received <paramref name="units"/> more stock.</summary>
    public void Receive(int units, string reason)
    {
        if (units <= 0) throw new ArgumentOutOfRangeException(nameof(units));
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        OnHandUnits += units;
    }

    /// <summary>
    /// Deduct <paramref name="units"/>. Returns the new on-hand. Goes negative when
    /// administered stock exceeds what's logged — administration is the source of truth so
    /// we surface the imbalance via the integration event rather than reject the call.
    /// </summary>
    public int Deduct(int units, string reason)
    {
        if (units <= 0) throw new ArgumentOutOfRangeException(nameof(units));
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        OnHandUnits -= units;
        if (OnHandUnits <= Threshold)
        {
            RaiseIntegrationEvent(new MedicationInventoryLowIntegrationEvent
            {
                InventoryItemId = Id,
                MedicationCodeSystem = Medication.CodeSystem,
                MedicationCode = Medication.Code,
                MedicationDisplay = Medication.DisplayName,
                LotNumber = LotNumber,
                OnHandUnits = OnHandUnits,
                Threshold = Threshold,
            });
        }
        return OnHandUnits;
    }

    /// <summary>Operator performed a physical count and adjusted the system to match.</summary>
    public void Adjust(int newOnHand, string reason)
    {
        if (newOnHand < 0) throw new ArgumentOutOfRangeException(nameof(newOnHand));
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        OnHandUnits = newOnHand;
    }
}
