using Dialysis.CQRS;
using Dialysis.HIS.PatientFlow.Features.AdmitPatient;
using Dialysis.HIS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Dialysis.HIS.Tests;

[Collection(nameof(HisFixtureCollection))]
public sealed class PatientFlowFlowTests(HisApiWebApplicationFactory factory)
{
    [Fact]
    public async Task AdmitPatient_persists_admission_with_ward_and_no_discharge()
    {
        using var scope = factory.Services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();
        var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();

        var patientId = Guid.CreateVersion7();
        var id = await gateway.SendCommandAsync<AdmitPatientCommand, Guid>(
            new AdmitPatientCommand(patientId, "WARD-A1"),
            CancellationToken.None);

        var admission = await db.Admissions.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, CancellationToken.None);
        admission.ShouldNotBeNull();
        admission.PatientId.ShouldBe(patientId);
        admission.Ward.Value.ShouldBe("WARD-A1");
        admission.DischargedAtUtc.ShouldBeNull();
    }
}
