using Dialysis.PDMS.Medications.Contracts;
using Dialysis.PDMS.Medications.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.Medications;

public sealed class MedicationAdministrationRecordTests
{
    [Fact]
    public void Recording_An_Administration_Adds_An_Entry_And_Raises_Event()
    {
        var mar = Build_Open_Mar();
        var entryId = Guid.CreateVersion7();

        mar.RecordAdministration(
            entryId,
            MedicationCoding.RxNorm("1234", "Heparin"),
            Dose.Units(1000),
            MedicationRoute.IntravenousPump,
            administeredAtUtc: DateTime.UtcNow,
            administeredBySub: "nurse:alice",
            relatedOrderId: null);

        mar.Entries.Count.ShouldBe(1);
        mar.Entries.First().Id.ShouldBe(entryId);
        mar.Entries.First().WasAdministered.ShouldBeTrue();
        mar.IntegrationEvents.OfType<MedicationAdministeredIntegrationEvent>()
            .ShouldHaveSingleItem()
            .EntryId.ShouldBe(entryId);
    }

    [Fact]
    public void Recording_A_Decline_Captures_The_Reason_And_Raises_Event()
    {
        var mar = Build_Open_Mar();
        var entryId = Guid.CreateVersion7();

        mar.RecordDecline(
            entryId,
            MedicationCoding.RxNorm("9999", "Insulin Glargine"),
            Dose.Units(20),
            MedicationRoute.Subcutaneous,
            declinedAtUtc: DateTime.UtcNow,
            declinedBySub: "nurse:alice",
            reason: "Patient refused.",
            relatedOrderId: null);

        var entry = mar.Entries.ShouldHaveSingleItem();
        entry.WasAdministered.ShouldBeFalse();
        entry.DeclineReason.ShouldBe("Patient refused.");
        mar.IntegrationEvents.OfType<MedicationDeclinedIntegrationEvent>()
            .ShouldHaveSingleItem()
            .Reason.ShouldBe("Patient refused.");
    }

    [Fact]
    public void Closing_The_Mar_Refuses_Further_Entries()
    {
        var mar = Build_Open_Mar();
        mar.Close(DateTime.UtcNow);

        Should.Throw<InvalidOperationException>(() =>
            mar.RecordAdministration(
                Guid.CreateVersion7(),
                MedicationCoding.RxNorm("1234", "Heparin"),
                Dose.Units(1000),
                MedicationRoute.IntravenousPump,
                administeredAtUtc: DateTime.UtcNow,
                administeredBySub: "nurse:alice",
                relatedOrderId: null));
    }

    [Fact]
    public void Closing_An_Already_Closed_Mar_Is_Idempotent()
    {
        var mar = Build_Open_Mar();
        var first = DateTime.UtcNow;
        mar.Close(first);

        // Second close shouldn't throw; ClosedAtUtc stays at the first value.
        mar.Close(first.AddMinutes(30));

        mar.Status.ShouldBe(MarStatus.Closed);
        mar.ClosedAtUtc.ShouldBe(first);
    }

    private static MedicationAdministrationRecord Build_Open_Mar() =>
        new(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), DateTime.UtcNow);
}
