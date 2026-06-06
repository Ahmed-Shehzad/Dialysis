using Dialysis.EHR.ClinicalNotes.SafetyChecks;
using Dialysis.EHR.PatientChart.Domain;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.Tests.SafetyChecks;

public sealed class ClinicalSafetyCheckerTests
{
    private static ClinicalSafetyChecker Checker(
        FakeAllergyRepository allergies,
        FakeMedicationStatementRepository medications,
        FakePrescriptionRepository prescriptions,
        FakeLabOrderRepository labOrders,
        DateTime nowUtc,
        ClinicalSafetyOptions? options = null) =>
        new(allergies, medications, prescriptions, labOrders,
            new FixedClock(nowUtc), Options.Create(options ?? new ClinicalSafetyOptions()));

    [Fact]
    public async Task Flags_Medication_Allergy_Conflict_By_Shared_Code_As_Blocking_Async()
    {
        var patient = Guid.NewGuid();
        var checker = Checker(
            new FakeAllergyRepository(SafetyTestData.Allergy(patient, "7980", "Penicillin G")),
            new FakeMedicationStatementRepository(),
            new FakePrescriptionRepository(),
            new FakeLabOrderRepository(),
            DateTime.UtcNow);

        var result = await checker.CheckPrescriptionAsync(patient, "7980", "Penicillin G sodium", CancellationToken.None);

        var advisory = result.Advisories.ShouldHaveSingleItem();
        advisory.Category.ShouldBe(AdvisoryCategory.MedicationAllergyConflict);
        advisory.Severity.ShouldBe(AdvisorySeverity.Blocking);
        result.HasBlocking.ShouldBeTrue();
    }

    [Fact]
    public async Task Flags_Medication_Allergy_Conflict_By_Display_Substring_Async()
    {
        var patient = Guid.NewGuid();
        var checker = Checker(
            new FakeAllergyRepository(SafetyTestData.Allergy(patient, "ALG-1", "Penicillin")),
            new FakeMedicationStatementRepository(),
            new FakePrescriptionRepository(),
            new FakeLabOrderRepository(),
            DateTime.UtcNow);

        // Different codes; the display "Penicillin" is a substring of "Penicillin V Potassium".
        var result = await checker.CheckPrescriptionAsync(patient, "RX-9", "Penicillin V Potassium", CancellationToken.None);

        result.Advisories.ShouldHaveSingleItem().Category.ShouldBe(AdvisoryCategory.MedicationAllergyConflict);
    }

    [Fact]
    public async Task Ignores_Refuted_Allergy_Async()
    {
        var patient = Guid.NewGuid();
        var checker = Checker(
            new FakeAllergyRepository(SafetyTestData.Allergy(patient, "7980", "Penicillin G", AllergyVerificationStatus.Refuted)),
            new FakeMedicationStatementRepository(),
            new FakePrescriptionRepository(),
            new FakeLabOrderRepository(),
            DateTime.UtcNow);

        var result = await checker.CheckPrescriptionAsync(patient, "7980", "Penicillin G sodium", CancellationToken.None);

        result.Advisories.ShouldBeEmpty();
    }

    [Fact]
    public async Task Allergy_Conflict_Severity_Is_Configurable_To_Warning_Async()
    {
        var patient = Guid.NewGuid();
        var checker = Checker(
            new FakeAllergyRepository(SafetyTestData.Allergy(patient, "7980", "Penicillin G")),
            new FakeMedicationStatementRepository(),
            new FakePrescriptionRepository(),
            new FakeLabOrderRepository(),
            DateTime.UtcNow,
            new ClinicalSafetyOptions { MedicationAllergyConflictBlocks = false });

        var result = await checker.CheckPrescriptionAsync(patient, "7980", "Penicillin G sodium", CancellationToken.None);

        result.Advisories.ShouldHaveSingleItem().Severity.ShouldBe(AdvisorySeverity.Warning);
        result.HasBlocking.ShouldBeFalse();
    }

    [Fact]
    public async Task Flags_Duplicate_Active_Medication_As_Warning_Async()
    {
        var patient = Guid.NewGuid();
        var checker = Checker(
            new FakeAllergyRepository(),
            new FakeMedicationStatementRepository(SafetyTestData.Medication(patient, "RX-100", "Lisinopril 10mg")),
            new FakePrescriptionRepository(),
            new FakeLabOrderRepository(),
            DateTime.UtcNow);

        var result = await checker.CheckPrescriptionAsync(patient, "RX-100", "Lisinopril 10mg", CancellationToken.None);

        var advisory = result.Advisories.ShouldHaveSingleItem();
        advisory.Category.ShouldBe(AdvisoryCategory.DuplicateActiveMedication);
        advisory.Severity.ShouldBe(AdvisorySeverity.Warning);
        advisory.SourceKind.ShouldBe("MedicationStatement");
        result.HasBlocking.ShouldBeFalse();
    }

    [Fact]
    public async Task Flags_Duplicate_Lab_Within_Window_And_Ignores_Outside_Async()
    {
        var patient = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;
        var checker = Checker(
            new FakeAllergyRepository(),
            new FakeMedicationStatementRepository(),
            new FakePrescriptionRepository(),
            new FakeLabOrderRepository(
                (SafetyTestData.LabOrder(patient, "2160-0"), nowUtc.AddHours(-1)),    // within 72h
                (SafetyTestData.LabOrder(patient, "2160-0"), nowUtc.AddHours(-100))), // outside 72h
            nowUtc);

        var result = await checker.CheckLabOrderAsync(patient, ["2160-0"], CancellationToken.None);

        var advisory = result.Advisories.ShouldHaveSingleItem();
        advisory.Category.ShouldBe(AdvisoryCategory.DuplicateLabOrder);
        advisory.Severity.ShouldBe(AdvisorySeverity.Warning);
        advisory.MatchedCode.ShouldBe("2160-0");
    }

    [Fact]
    public async Task No_Advisory_For_An_Unrelated_Order_Async()
    {
        var patient = Guid.NewGuid();
        var checker = Checker(
            new FakeAllergyRepository(SafetyTestData.Allergy(patient, "7980", "Penicillin G")),
            new FakeMedicationStatementRepository(SafetyTestData.Medication(patient, "RX-100", "Lisinopril 10mg")),
            new FakePrescriptionRepository(),
            new FakeLabOrderRepository(),
            DateTime.UtcNow);

        var result = await checker.CheckPrescriptionAsync(patient, "RX-200", "Atorvastatin 20mg", CancellationToken.None);

        result.Advisories.ShouldBeEmpty();
    }
}
