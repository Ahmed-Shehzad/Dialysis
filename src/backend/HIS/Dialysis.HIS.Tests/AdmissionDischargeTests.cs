using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;
using Dialysis.HIS.PatientFlow.Domain;
using Dialysis.HIS.PatientFlow.Domain.ValueObjects;
using Shouldly;

namespace Dialysis.HIS.Tests;

public sealed class AdmissionDischargeTests
{
    [Fact]
    public void Discharge_Sets_The_Timestamp_And_Raises_The_Discharged_Event_Once()
    {
        var admission = Admission.Admit(Guid.NewGuid(), new WardCode("4N"), DateTime.UtcNow);
        var dischargedAt = DateTime.UtcNow.AddHours(6);

        admission.Discharge(dischargedAt);

        admission.DischargedAtUtc.ShouldBe(dischargedAt);
        var evt = admission.IntegrationEvents.OfType<PatientDischargedIntegrationEvent>().ShouldHaveSingleItem();
        evt.AdmissionId.ShouldBe(admission.Id);
        evt.PatientId.ShouldBe(admission.PatientId);
        evt.WardCode.ShouldBe("4N");
        evt.DischargedAtUtc.ShouldBe(dischargedAt);
        evt.SchemaVersion.ShouldBe(1);
    }

    [Fact]
    public void Cannot_Discharge_Twice()
    {
        var admission = Admission.Admit(Guid.NewGuid(), new WardCode("4N"), DateTime.UtcNow);
        admission.Discharge(DateTime.UtcNow);
        Should.Throw<DomainException>(() => admission.Discharge(DateTime.UtcNow));
    }
}
