using Dialysis.BuildingBlocks.DataProtection.LawfulBases;
using Dialysis.BuildingBlocks.DataProtection.Retention;
using Dialysis.BuildingBlocks.DataProtection.Ropa;
using Xunit;

namespace Dialysis.BuildingBlocks.DataProtection.Tests;

/// <summary>
/// The operator dashboard binds to <see cref="RopaView"/>, not the domain document: enums must
/// arrive humanised, the <c>[Flags]</c> data categories expanded into a string list, and each
/// activity's retention key resolved to a window label. These assertions are the contract the
/// SPA's RoPA viewer depends on.
/// </summary>
public sealed class RopaViewTests
{
    private static RopaDocument SampleDocument()
    {
        var retention = new[]
        {
            new RetentionWindowRegistration(
                "clinical.record",
                new RetentionWindow(
                    TimeSpan.FromDays(365.25 * 10),
                    TimeSpan.FromDays(365.25 * 30),
                    "DE Berufsordnung §10"),
                "Patient charts, treatment sessions."),
            new RetentionWindowRegistration(
                "billing.record",
                new RetentionWindow(
                    TimeSpan.FromDays(365.25 * 10),
                    TimeSpan.FromDays(365.25 * 10),
                    "DE HGB §257"),
                "Claims, charge captures."),
        };

        var activities = new[]
        {
            new ProcessingActivity(
                ActivityName: "pdms.medications.administer",
                Basis: LawfulBasis.HealthcareProvision,
                Categories: DataCategory.ClinicalHealth | DataCategory.Medication,
                Purpose: "Record administration at the chair.",
                RetentionKey: "clinical.record",
                RecipientCategories: ["EHR (MedicationStatement update)"]),
            new ProcessingActivity(
                ActivityName: "pdms.telemetry.unkeyed",
                Basis: LawfulBasis.LegitimateInterests,
                Categories: DataCategory.DeviceTelemetry,
                Purpose: "Telemetry without a retention key.",
                RetentionKey: null,
                RecipientCategories: []),
        };

        return new RopaDocument(
            ControllerName: "Test Clinic GmbH",
            ControllerContact: "dpo@test-clinic.de",
            GeneratedAtUtc: DateTimeOffset.UnixEpoch,
            Modules: [new RopaModuleSection("pdms", activities)],
            Retention: retention);
    }

    [Fact]
    public void From_Humanises_Basis_And_Expands_Category_Flags()
    {
        var view = RopaView.From(SampleDocument());

        var activity = view.Modules.Single().Activities[0];
        Assert.Equal("pdms.medications.administer", activity.Name);
        Assert.Contains("Art. 6(1)(e)", activity.LawfulBasis);
        Assert.Equal(["Clinical health", "Medication"], activity.DataCategories);
        Assert.Equal(["EHR (MedicationStatement update)"], activity.Recipients);
    }

    [Fact]
    public void From_Resolves_Retention_Key_To_Window_Label()
    {
        var view = RopaView.From(SampleDocument());
        var activities = view.Modules.Single().Activities;

        Assert.Equal("10–30 years", activities[0].RetentionWindow);
        Assert.Null(activities[1].RetentionWindow);
    }

    [Fact]
    public void From_Projects_Retention_Schedule_With_Formatted_Windows()
    {
        var view = RopaView.From(SampleDocument());

        var clinical = view.Retention.Single(r => r.DataCategory == "Patient charts, treatment sessions.");
        Assert.Equal("10–30 years", clinical.WindowLabel);
        Assert.Equal("DE Berufsordnung §10", clinical.LegalBasis);

        var billing = view.Retention.Single(r => r.DataCategory == "Claims, charge captures.");
        Assert.Equal("10 years", billing.WindowLabel);
    }
}
