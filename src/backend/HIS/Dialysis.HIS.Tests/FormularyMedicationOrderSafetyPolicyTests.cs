using Dialysis.HIS.Medication;

namespace Dialysis.HIS.Tests;

public sealed class FormularyMedicationOrderSafetyPolicyTests
{
    private readonly FormularyMedicationOrderSafetyPolicy _policy = new();

    [Fact]
    public void Throws_for_blocked_demonstration_code()
    {
        Assert.Throws<InvalidOperationException>(() => _policy.EnsureCanPlace(Guid.NewGuid(), FormularyMedicationOrderSafetyPolicy.BlockedDemonstrationCode));
    }

    [Fact]
    public void Allows_normal_medication_code()
    {
        _policy.EnsureCanPlace(Guid.NewGuid(), "ASPIRIN-81");
    }
}
