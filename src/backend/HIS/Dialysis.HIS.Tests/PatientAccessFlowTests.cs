using Dialysis.CQRS;
using Dialysis.HIS.PatientAccess.Features.GetPatientPortalSummary;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Dialysis.HIS.Tests;

[Collection(nameof(HisFixtureCollection))]
public sealed class PatientAccessFlowTests(HisApiWebApplicationFactory factory)
{
    [Fact]
    public async Task GetPatientPortalSummary_returns_zero_counts_for_unknown_patient()
    {
        using var scope = factory.Services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();

        var patientId = Guid.CreateVersion7();
        var dto = await gateway.SendQueryAsync<GetPatientPortalSummaryQuery, PatientPortalSummaryDto>(
            new GetPatientPortalSummaryQuery(patientId),
            CancellationToken.None);

        dto.PatientId.ShouldBe(patientId);
        dto.UpcomingAppointmentCount.ShouldBe(0);
        dto.OpenMedicationOrderCount.ShouldBe(0);
        dto.OpenAdmissionCount.ShouldBe(0);
    }
}
