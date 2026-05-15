using Dialysis.EHR.Registration.Domain;
using Dialysis.EHR.Registration.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Stores;

public sealed class PatientRepository(EhrDbContext db) : IPatientRepository
{
    public Task<Patient?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Patients.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Patient?> FindByMedicalRecordNumberAsync(string medicalRecordNumber, CancellationToken cancellationToken = default) =>
        db.Patients.FirstOrDefaultAsync(p => p.MedicalRecordNumber == medicalRecordNumber, cancellationToken);

    public async Task<IReadOnlyList<Patient>> SearchAsync(string? nameFragment, int take, CancellationToken cancellationToken = default)
    {
        var query = db.Patients.AsQueryable();
        if (!string.IsNullOrWhiteSpace(nameFragment))
        {
            var pattern = $"%{nameFragment.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Name.FamilyName, pattern) ||
                EF.Functions.ILike(p.Name.GivenName, pattern) ||
                EF.Functions.ILike(p.MedicalRecordNumber, pattern));
        }

        return await query
            .OrderBy(p => p.Name.FamilyName)
            .ThenBy(p => p.Name.GivenName)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public void Add(Patient patient) => db.Patients.Add(patient);
}

public sealed class ProviderRepository(EhrDbContext db) : IProviderRepository
{
    public Task<Provider?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Providers.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Provider?> FindByNpiAsync(string npi, CancellationToken cancellationToken = default) =>
        db.Providers.FirstOrDefaultAsync(p => p.NationalProviderIdentifier == npi, cancellationToken);

    public void Add(Provider provider) => db.Providers.Add(provider);
}
