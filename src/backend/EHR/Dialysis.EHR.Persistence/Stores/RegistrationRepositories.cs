using Dialysis.EHR.Registration.Domain;
using Dialysis.EHR.Registration.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Stores;

public sealed class PatientRepository : IPatientRepository
{
    private readonly EhrDbContext _db;
    public PatientRepository(EhrDbContext db) => _db = db;
    public Task<Patient?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Patients.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Patient?> FindByMedicalRecordNumberAsync(string medicalRecordNumber, CancellationToken cancellationToken = default) =>
        _db.Patients.FirstOrDefaultAsync(p => p.MedicalRecordNumber == medicalRecordNumber, cancellationToken);

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
        var query = _db.Patients.AsQueryable();

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

        if (criteria.DateOfBirthFrom is { } dobFrom)
            query = query.Where(p => p.DateOfBirth >= dobFrom);

        if (criteria.DateOfBirthTo is { } dobTo)
            query = query.Where(p => p.DateOfBirth <= dobTo);

        if (!string.IsNullOrWhiteSpace(criteria.SexAtBirthCode))
        {
            var code = criteria.SexAtBirthCode.Trim();
            query = query.Where(p => p.SexAtBirthCode == code);
        }

        if (criteria.Status is { } status)
            query = query.Where(p => p.Status == status);

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

    public void Add(Patient patient) => _db.Patients.Add(patient);

    public IAsyncEnumerable<Patient> StreamAllAsync(DateTimeOffset? since, CancellationToken cancellationToken = default)
    {
        var query = _db.Patients.AsNoTracking().OrderBy(p => p.MedicalRecordNumber).AsQueryable();
        if (since is { } cutoff)
        {
            query = query.Where(p => p.UpdatedAtUtc >= cutoff);
        }
        return query.AsAsyncEnumerable();
    }
}

public sealed class ProviderRepository : IProviderRepository
{
    private readonly EhrDbContext _db;
    public ProviderRepository(EhrDbContext db) => _db = db;
    public Task<Provider?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Providers.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Provider?> FindByNpiAsync(string npi, CancellationToken cancellationToken = default) =>
        _db.Providers.FirstOrDefaultAsync(p => p.NationalProviderIdentifier == npi, cancellationToken);

    public void Add(Provider provider) => _db.Providers.Add(provider);
}
