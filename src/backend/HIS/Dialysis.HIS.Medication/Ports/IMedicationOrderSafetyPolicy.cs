namespace Dialysis.HIS.Medication.Ports;

/// <summary>
/// Cross-checks medication orders before persistence (formulary / interaction stubs toward pharmacy safety).
/// </summary>
public interface IMedicationOrderSafetyPolicy
{
    void EnsureCanPlace(Guid patientId, string medicationCode);
}
