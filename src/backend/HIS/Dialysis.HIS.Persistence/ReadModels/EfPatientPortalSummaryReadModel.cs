using Dialysis.HIS.PatientAccess.Ports;
using Dialysis.HIS.PatientFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.ReadModels;

public sealed class EfPatientPortalSummaryReadModel(HisDbContext db) : IPatientPortalSummaryReadModel
{
    public async Task<PatientPortalSummaryDto?> GetAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var row = await db.Patients.AsNoTracking()
            .Where(p => p.Id == patientId)
            .Select(p => new { p.Id, p.MedicalRecordNumber, p.VisitState })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return row is null
            ? null
            : new PatientPortalSummaryDto(row.Id, row.MedicalRecordNumber, row.VisitState.ToString(), "Portal summary (stub).");
    }
}
