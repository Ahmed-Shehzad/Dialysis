using Dialysis.Domain.Entities;
using Dialysis.SharedKernel.ValueObjects;
using Shouldly;
using Xunit;

namespace Dialysis.Tests.Features.Patients;

public sealed class PatientEntityTests
{
    [Fact]
    public void Create_returns_patient_with_provided_values()
    {
        var tenantId = new TenantId("default");
        var logicalId = new PatientId("patient-001");
        var birthDate = new DateTime(1990, 5, 20);

        var patient = Patient.Create(tenantId, logicalId, "Doe", "Jane", birthDate);

        patient.TenantId.ShouldBe(tenantId);
        patient.LogicalId.ShouldBe(logicalId);
        patient.FamilyName.ShouldBe("Doe");
        patient.GivenNames.ShouldBe("Jane");
        patient.BirthDate.ShouldBe(birthDate);
    }

    [Fact]
    public void Update_sets_non_null_values()
    {
        var tenantId = new TenantId("default");
        var logicalId = new PatientId("patient-001");
        var patient = Patient.Create(tenantId, logicalId, "Smith", "John", new DateTime(1985, 1, 1));

        patient.Update("Jones", "Jane", new DateTime(1990, 6, 15));

        patient.FamilyName.ShouldBe("Jones");
        patient.GivenNames.ShouldBe("Jane");
        patient.BirthDate.ShouldBe(new DateTime(1990, 6, 15));
    }

    [Fact]
    public void Update_preserves_values_when_null_passed()
    {
        var tenantId = new TenantId("default");
        var logicalId = new PatientId("patient-001");
        var patient = Patient.Create(tenantId, logicalId, "Smith", "John", new DateTime(1985, 1, 1));

        patient.Update(null, null, null);

        patient.FamilyName.ShouldBe("Smith");
        patient.GivenNames.ShouldBe("John");
        patient.BirthDate.ShouldBe(new DateTime(1985, 1, 1));
    }
}
