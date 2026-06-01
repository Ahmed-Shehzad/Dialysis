using Dialysis.BuildingBlocks.DataProtection.LawfulBases;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.BuildingBlocks.DataProtection.Tests;

/// <summary>
/// Verifies the lawful-basis registry is the gate every command must pass through. Each
/// module declares its activities once at startup; commands at runtime trigger
/// <see cref="ILawfulBasisRegistry.IsAuthorised"/> to decide whether to short-circuit with
/// a 403.
/// </summary>
public sealed class LawfulBasisRegistryTests
{
    [Fact]
    public void Registry_Authorises_Exact_Match_Of_Basis_And_Categories()
    {
        var services = new ServiceCollection()
            .AddEuDataProtection("pdms", r => r.RegisterActivity(
                "pdms.medications.administer",
                LawfulBasis.HealthcareProvision,
                DataCategory.ClinicalHealth | DataCategory.Medication,
                purpose: "Record what was administered at the chair."))
            .BuildServiceProvider();

        var registry = services.GetRequiredService<ILawfulBasisRegistry>();

        Assert.Equal("pdms", registry.ModuleSlug);
        Assert.True(registry.IsAuthorised(
            LawfulBasis.HealthcareProvision,
            DataCategory.ClinicalHealth | DataCategory.Medication));
    }

    [Fact]
    public void Registry_Rejects_Activity_With_Different_Basis()
    {
        var services = new ServiceCollection()
            .AddEuDataProtection("pdms", r => r.RegisterActivity(
                "pdms.medications.administer",
                LawfulBasis.HealthcareProvision,
                DataCategory.ClinicalHealth,
                purpose: "Record administration."))
            .BuildServiceProvider();

        var registry = services.GetRequiredService<ILawfulBasisRegistry>();

        Assert.False(registry.IsAuthorised(
            LawfulBasis.LegitimateInterests, DataCategory.ClinicalHealth));
    }

    [Fact]
    public void Registry_Rejects_Activity_Missing_A_Required_Category()
    {
        var services = new ServiceCollection()
            .AddEuDataProtection("pdms", r => r.RegisterActivity(
                "pdms.vitals.read",
                LawfulBasis.HealthcareProvision,
                DataCategory.DeviceTelemetry,
                purpose: "Read live vitals."))
            .BuildServiceProvider();

        var registry = services.GetRequiredService<ILawfulBasisRegistry>();

        // Caller asks for DeviceTelemetry + ClinicalHealth; only DeviceTelemetry is registered.
        Assert.False(registry.IsAuthorised(
            LawfulBasis.HealthcareProvision,
            DataCategory.DeviceTelemetry | DataCategory.ClinicalHealth));
    }

    [Fact]
    public void Activities_Survive_Round_Trip_For_Ropa()
    {
        var services = new ServiceCollection()
            .AddEuDataProtection("pdms", r =>
            {
                r.RegisterActivity(
                    "pdms.medications.administer",
                    LawfulBasis.HealthcareProvision,
                    DataCategory.ClinicalHealth | DataCategory.Medication,
                    purpose: "Record what was administered at the chair.",
                    retentionKey: "clinical.record",
                    recipientCategories: ["EHR (MedicationStatement update)"]);
                r.RegisterActivity(
                    "pdms.billing.charge-ready",
                    LawfulBasis.LegalObligation,
                    DataCategory.ClinicalHealth | DataCategory.Financial,
                    purpose: "Emit a session-completed billable fact.",
                    retentionKey: "billing.record",
                    recipientCategories: ["EHR (Charge creation)"]);
            })
            .BuildServiceProvider();

        var registry = services.GetRequiredService<ILawfulBasisRegistry>();

        Assert.Equal(2, registry.Activities.Count);
        var admin = Assert.Single(registry.Activities, a => a.ActivityName == "pdms.medications.administer");
        Assert.Equal("clinical.record", admin.RetentionKey);
        Assert.Contains("EHR (MedicationStatement update)", admin.RecipientCategories);
    }
}
