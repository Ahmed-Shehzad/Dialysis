using Dialysis.HIS.Medication.Ports;

namespace Dialysis.HIS.Medication;

/// <summary>
/// Stub formulary: blocks a sentinel code to demonstrate safety gating (replace with formulary / interaction service).
/// </summary>
public sealed class FormularyMedicationOrderSafetyPolicy : IMedicationOrderSafetyPolicy
{
    public const string BlockedDemonstrationCode = "BLACKLIST-DEMO";

    public void EnsureCanPlace(Guid patientId, string medicationCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(medicationCode);
        if (string.Equals(medicationCode, BlockedDemonstrationCode, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Medication '{medicationCode}' is blocked by formulary policy (demonstration stub).");
    }
}
