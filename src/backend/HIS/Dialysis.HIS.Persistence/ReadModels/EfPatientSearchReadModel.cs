using Dialysis.HIS.DataServices.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.ReadModels;

public sealed class EfPatientSearchReadModel(HisDbContext db) : IPatientSearchReadModel
{
    public async Task<IReadOnlyList<PatientSearchResultDto>> SearchAsync(string? mrnContains, CancellationToken cancellationToken = default)
    {
        var q = db.Patients.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(mrnContains))
            q = q.Where(p => p.MedicalRecordNumber.Contains(mrnContains));

        return await q
            .OrderBy(p => p.MedicalRecordNumber)
            .Select(p => new PatientSearchResultDto(p.Id, p.MedicalRecordNumber, p.VisitState.ToString()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
