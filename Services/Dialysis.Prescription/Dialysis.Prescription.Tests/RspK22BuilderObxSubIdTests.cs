using BuildingBlocks.Tenancy;
using BuildingBlocks.ValueObjects;

using Dialysis.Prescription.Application.Abstractions;
using Dialysis.Prescription.Application.Domain;
using Dialysis.Prescription.Infrastructure.Hl7;

using Shouldly;

using PrescriptionEntity = Dialysis.Prescription.Application.Domain.Prescription;

namespace Dialysis.Prescription.Tests;

/// <summary>
/// Verifies OBX-4 sub-ID: when ProfileSetting.SubId is null, RspK22Builder uses MdcToObxSubIdCatalog.
/// </summary>
public sealed class RspK22BuilderObxSubIdTests
{
    [Fact]
    public void BuildFromPrescription_SettingWithoutSubId_UsesCatalogSubId()
    {
        var prescription = PrescriptionEntity.Create(
            "ORD001",
            new MedicalRecordNumber("MRN123"),
            "HD",
            "DR_SMITH",
            null,
            TenantContext.DefaultTenantId);

        prescription.AddSetting(ProfileSetting.Constant("MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING", 300, null, "RSET"));
        prescription.AddSetting(ProfileSetting.Constant("MDC_HDIALY_UF_RATE_SETTING", 500, null, "RSET"));

        var context = new RspK22ValidationContext("TEST001", null, null);
        var builder = new RspK22Builder();
        string hl7 = builder.BuildFromPrescription(prescription, context);

        hl7.ShouldContain("1.1.3.1"); // Blood pump sub-ID
        hl7.ShouldContain("1.1.9.2"); // UF rate sub-ID
        hl7.ShouldContain("OBX|");
        hl7.ShouldContain("300");
        hl7.ShouldContain("500");
    }

    [Fact]
    public void BuildFromPrescription_SettingWithExplicitSubId_UsesExplicitSubId()
    {
        var prescription = PrescriptionEntity.Create(
            "ORD002",
            new MedicalRecordNumber("MRN456"),
            "HD",
            null,
            null,
            TenantContext.DefaultTenantId);

        prescription.AddSetting(ProfileSetting.Constant("MDC_HDIALY_UF_RATE_SETTING", 400, "2.2.2.2", "RSET"));

        var context = new RspK22ValidationContext(null, null, null);
        var builder = new RspK22Builder();
        string hl7 = builder.BuildFromPrescription(prescription, context);

        hl7.ShouldContain("2.2.2.2");
    }

    [Fact]
    public void BuildFromPrescription_UnknownCode_UsesDefaultSubId()
    {
        var prescription = PrescriptionEntity.Create(
            "ORD003",
            new MedicalRecordNumber("MRN789"),
            "HD",
            null,
            null,
            TenantContext.DefaultTenantId);

        prescription.AddSetting(ProfileSetting.Constant("MDC_UNKNOWN_CODE", 99, null, "RSET"));

        var context = new RspK22ValidationContext(null, null, null);
        var builder = new RspK22Builder();
        string hl7 = builder.BuildFromPrescription(prescription, context);

        hl7.ShouldContain("1.1.9.1"); // Default fallback
        hl7.ShouldContain("99");
    }
}
