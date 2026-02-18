using BuildingBlocks.ValueObjects;

using Dialysis.Patient.Application.Abstractions;
using Dialysis.Patient.Application.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using PatientDomain = Dialysis.Patient.Application.Domain.Patient;

namespace Dialysis.Patient.Infrastructure.Persistence;

public sealed class PatientRepository : IPatientRepository
{
    private readonly PatientDbContext _db;

    public PatientRepository(PatientDbContext db)
    {
        _db = db;
    }

    public async Task<PatientDomain?> GetByMrnAsync(MedicalRecordNumber mrn, CancellationToken cancellationToken = default)
    {
        return await _db.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.MedicalRecordNumber == mrn, cancellationToken);
    }

    public async Task<IReadOnlyList<PatientDomain>> SearchByNameAsync(PersonName name, CancellationToken cancellationToken = default)
    {
        return await _db.Patients
            .AsNoTracking()
            .Where(p => p.Name.FirstName == name.FirstName && p.Name.LastName == name.LastName)
            .ToListAsync(cancellationToken);
    }

    public async Task<PatientDomain> AddAsync(PatientDomain patient, CancellationToken cancellationToken = default)
    {
        _ = _db.Patients.Add(patient);
        _ = await _db.SaveChangesAsync(cancellationToken);
        return patient;
    }
}
