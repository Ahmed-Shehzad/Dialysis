using Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;

namespace Dialysis.Persistence;

public sealed class PatientRepository : IPatientRepository
{
    private readonly DialysisDbContext _db;

    public PatientRepository(DialysisDbContext db) => _db = db;

    public async Task AddAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        _db.Patients.Update(patient);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        _db.Patients.Remove(patient);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
