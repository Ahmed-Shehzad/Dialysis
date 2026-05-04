using Dialysis.HIS.PatientFlow.Domain;
using Dialysis.HIS.PatientFlow.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfPatientRepository(HisDbContext db) : IPatientRepository
{
    public void Add(Patient patient) => db.Patients.Add(patient);

    public Task<Patient?> FindByMedicalRecordNumberAsync(string mrn, CancellationToken cancellationToken = default) =>
        db.Patients.FirstOrDefaultAsync(p => p.MedicalRecordNumber == mrn, cancellationToken);

    public Task<Patient?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Patients.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
}
