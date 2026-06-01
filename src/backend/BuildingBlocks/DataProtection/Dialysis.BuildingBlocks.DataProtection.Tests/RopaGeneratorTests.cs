using Dialysis.BuildingBlocks.DataProtection.LawfulBases;
using Dialysis.BuildingBlocks.DataProtection.Ropa;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.BuildingBlocks.DataProtection.Tests;

/// <summary>
/// RoPA must stitch every module's registered activities + the platform's retention windows
/// into one document. Verifies the generator's output is shaped correctly for the operator
/// dashboard's RoPA viewer.
/// </summary>
public sealed class RopaGeneratorTests
{
    [Fact]
    public void Ropa_Stitches_Activities_From_Every_Registered_Module()
    {
        var services = new ServiceCollection();
        services.AddEuDataProtection("pdms", r => r.RegisterActivity(
            "pdms.medications.administer",
            LawfulBasis.HealthcareProvision,
            DataCategory.ClinicalHealth | DataCategory.Medication,
            purpose: "Record administration at the chair."));
        services.AddEuDataProtection("ehr", r => r.RegisterActivity(
            "ehr.billing.charge-capture",
            LawfulBasis.LegalObligation,
            DataCategory.ClinicalHealth | DataCategory.Financial,
            purpose: "Capture a billable charge after a session."));
        services.Configure<RopaOptions>(o =>
        {
            o.ControllerName = "Test Clinic GmbH";
            o.ControllerContact = "dpo@test-clinic.de";
        });

        var sp = services.BuildServiceProvider();
        var generator = sp.GetRequiredService<IRopaGenerator>();
        var doc = generator.Generate();

        Assert.Equal("Test Clinic GmbH", doc.ControllerName);
        Assert.Equal(2, doc.Modules.Count);
        Assert.Single(doc.Modules, m => m.ModuleSlug == "pdms");
        Assert.Single(doc.Modules, m => m.ModuleSlug == "ehr");
        Assert.NotEmpty(doc.Retention);
        Assert.Contains(doc.Retention, r => r.Key == "clinical.record");
        Assert.Contains(doc.Retention, r => r.Key == "billing.record");
    }
}
