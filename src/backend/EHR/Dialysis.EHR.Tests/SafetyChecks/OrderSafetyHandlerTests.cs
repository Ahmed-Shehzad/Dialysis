using Dialysis.EHR.ClinicalNotes.Features.OrderLabTest;
using Dialysis.EHR.ClinicalNotes.Features.OrderPrescription;
using Dialysis.EHR.ClinicalNotes.SafetyChecks;
using Dialysis.EHR.Contracts.Integration;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.Tests.SafetyChecks;

public sealed class OrderSafetyHandlerTests
{
    private static SafetyAdvisory BlockingAllergy() => new(
        AdvisoryCategory.MedicationAllergyConflict, AdvisorySeverity.Blocking,
        "7980", "Penicillin G", "Penicillin G sodium", Guid.NewGuid(), "Allergy");

    private static SafetyAdvisory DuplicateMedWarning() => new(
        AdvisoryCategory.DuplicateActiveMedication, AdvisorySeverity.Warning,
        "RX-100", "Lisinopril 10mg", "Lisinopril 10mg", Guid.NewGuid(), "MedicationStatement");

    private static OrderPrescriptionCommand Prescribe(
        Guid patient, bool acknowledge = false, string? reason = null, string? by = null) =>
        new(patient, Guid.NewGuid(), Guid.NewGuid(), "7980", "Penicillin G sodium",
            "1 tab", "daily", 30, 0, "PHARM-1",
            AcknowledgeAdvisories: acknowledge, OverrideReason: reason, OverriddenBy: by);

    [Fact]
    public async Task Prescription_Blocks_Unacknowledged_Conflict_And_Persists_Nothing_Async()
    {
        var repo = new FakePrescriptionRepository();
        var uow = new FakeUnitOfWork();
        var handler = new OrderPrescriptionCommandHandler(
            repo, new FakeSafetyChecker(new SafetyAdvisoryResult([BlockingAllergy()])), uow);

        var ex = await Should.ThrowAsync<ClinicalSafetyBlockedException>(() =>
            handler.HandleAsync(Prescribe(Guid.NewGuid()), CancellationToken.None));

        ex.Advisories.ShouldHaveSingleItem().Category.ShouldBe(AdvisoryCategory.MedicationAllergyConflict);
        repo.Added.ShouldBeNull();
        uow.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Prescription_Override_Persists_Reason_And_Emits_V2_Event_Async()
    {
        var repo = new FakePrescriptionRepository();
        var handler = new OrderPrescriptionCommandHandler(
            repo, new FakeSafetyChecker(new SafetyAdvisoryResult([BlockingAllergy()])), new FakeUnitOfWork());

        var result = await handler.HandleAsync(
            Prescribe(Guid.NewGuid(), acknowledge: true, reason: "Documented tolerance — prior course completed", by: "provider-7"),
            CancellationToken.None);

        result.Advisories.ShouldHaveSingleItem();
        var rx = repo.Added.ShouldNotBeNull();
        rx.OverrideReason.ShouldBe("Documented tolerance — prior course completed");
        rx.OverriddenBy.ShouldBe("provider-7");

        var evt = rx.IntegrationEvents.OfType<PrescriptionOrderedIntegrationEvent>().ShouldHaveSingleItem();
        evt.SchemaVersion.ShouldBe(2);
        evt.OverrideReason.ShouldBe("Documented tolerance — prior course completed");
        evt.OverriddenBy.ShouldBe("provider-7");
    }

    [Fact]
    public async Task Prescription_Acknowledge_Without_Reason_Still_Blocks_Async()
    {
        var repo = new FakePrescriptionRepository();
        var handler = new OrderPrescriptionCommandHandler(
            repo, new FakeSafetyChecker(new SafetyAdvisoryResult([BlockingAllergy()])), new FakeUnitOfWork());

        await Should.ThrowAsync<ClinicalSafetyBlockedException>(() =>
            handler.HandleAsync(Prescribe(Guid.NewGuid(), acknowledge: true, reason: "   "), CancellationToken.None));
        repo.Added.ShouldBeNull();
    }

    [Fact]
    public async Task Prescription_Warning_Only_Proceeds_Without_Override_Async()
    {
        var repo = new FakePrescriptionRepository();
        var handler = new OrderPrescriptionCommandHandler(
            repo, new FakeSafetyChecker(new SafetyAdvisoryResult([DuplicateMedWarning()])), new FakeUnitOfWork());

        var result = await handler.HandleAsync(Prescribe(Guid.NewGuid()), CancellationToken.None);

        result.Advisories.ShouldHaveSingleItem().Severity.ShouldBe(AdvisorySeverity.Warning);
        var rx = repo.Added.ShouldNotBeNull();
        rx.OverrideReason.ShouldBeNull();
    }

    [Fact]
    public async Task Lab_Order_Duplicate_Is_Non_Blocking_Warning_Async()
    {
        var repo = new FakeLabOrderRepository();
        var dup = new SafetyAdvisory(
            AdvisoryCategory.DuplicateLabOrder, AdvisorySeverity.Warning,
            "2160-0", "2160-0", "2160-0", Guid.NewGuid(), "LabOrder");
        var handler = new OrderLabTestCommandHandler(
            repo, new FakeSafetyChecker(new SafetyAdvisoryResult([dup])), new FakeUnitOfWork());

        var result = await handler.HandleAsync(
            new OrderLabTestCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "LAB-1", ["2160-0"]),
            CancellationToken.None);

        result.Advisories.ShouldHaveSingleItem().Category.ShouldBe(AdvisoryCategory.DuplicateLabOrder);
        repo.Added.ShouldNotBeNull();
    }
}
