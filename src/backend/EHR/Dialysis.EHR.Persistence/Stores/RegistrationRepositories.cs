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
        var page = await SearchAsync(
            new PatientSearchCriteria(nameFragment, null, null, null, null, null, null, null, 0, take),
            cancellationToken).ConfigureAwait(false);
        return page.Items;
    }

    public async Task<PatientSearchPage> SearchAsync(
        PatientSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = db.Patients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Query))
        {
            var pattern = $"%{criteria.Query.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Name.FamilyName, pattern) ||
                EF.Functions.ILike(p.Name.GivenName, pattern) ||
                EF.Functions.ILike(p.MedicalRecordNumber, pattern));
        }

        if (!string.IsNullOrWhiteSpace(criteria.FamilyName))
        {
            var pattern = $"%{criteria.FamilyName.Trim()}%";
            query = query.Where(p => EF.Functions.ILike(p.Name.FamilyName, pattern));
        }

        if (!string.IsNullOrWhiteSpace(criteria.GivenName))
        {
            var pattern = $"%{criteria.GivenName.Trim()}%";
            query = query.Where(p => EF.Functions.ILike(p.Name.GivenName, pattern));
        }

        if (!string.IsNullOrWhiteSpace(criteria.MedicalRecordNumber))
        {
            var pattern = $"%{criteria.MedicalRecordNumber.Trim()}%";
            query = query.Where(p => EF.Functions.ILike(p.MedicalRecordNumber, pattern));
        }

        if (criteria.DateOfBirthFrom.HasValue)
            query = query.Where(p => p.DateOfBirth >= criteria.DateOfBirthFrom.Value);

        if (criteria.DateOfBirthTo.HasValue)
            query = query.Where(p => p.DateOfBirth <= criteria.DateOfBirthTo.Value);

        if (!string.IsNullOrWhiteSpace(criteria.SexAtBirthCode))
        {
            var code = criteria.SexAtBirthCode.Trim();
            query = query.Where(p => p.SexAtBirthCode == code);
        }

        if (criteria.Status.HasValue)
            query = query.Where(p => p.Status == criteria.Status.Value);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var skip = Math.Max(0, criteria.Skip);
        var take = Math.Clamp(criteria.Take, 1, 200);

        var items = await query
            .OrderBy(p => p.Name.FamilyName)
            .ThenBy(p => p.Name.GivenName)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PatientSearchPage(items, total);
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
