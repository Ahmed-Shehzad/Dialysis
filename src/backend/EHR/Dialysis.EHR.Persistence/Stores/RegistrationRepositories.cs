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
