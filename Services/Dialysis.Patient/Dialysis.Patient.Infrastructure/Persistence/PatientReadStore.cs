using Dialysis.Patient.Application.Abstractions;
using Dialysis.Patient.Infrastructure.Persistence;
using Dialysis.Patient.Infrastructure.ReadModels;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Patient.Infrastructure;

public sealed class PatientReadStore : IPatientReadStore
{
    private readonly PatientReadDbContext _db;

    public PatientReadStore(PatientReadDbContext db)
    {
        _db = db;
    }

    public async Task<PatientReadDto?> GetByMrnAsync(string tenantId, string mrn, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mrn)) return null;
        PatientReadModel? p = await _db.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.MedicalRecordNumber == mrn, cancellationToken);
        return ToDto(p);
    }

    public async Task<PatientReadDto?> GetByIdAsync(string tenantId, string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        PatientReadModel? p = await _db.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, cancellationToken);
        return ToDto(p);
    }

    public async Task<IReadOnlyList<PatientReadDto>> GetAllForTenantAsync(string tenantId, int limit, CancellationToken cancellationToken = default)
    {
        List<PatientReadModel> list = await _db.Patients
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Take(Math.Max(1, Math.Min(limit, 1000)))
            .ToListAsync(cancellationToken);
        return list.Select(ToDtoNonNull).ToList();
    }

    public async Task<IReadOnlyList<PatientReadDto>> SearchAsync(string tenantId, string? identifier, string? familyName, string? givenName, DateOnly? birthdate, int limit, CancellationToken cancellationToken = default)
    {
        IQueryable<PatientReadModel> query = _db.Patients.AsNoTracking().Where(x => x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(identifier))
            query = query.Where(x => x.MedicalRecordNumber.Contains(identifier) || (x.PersonNumber != null && x.PersonNumber.Contains(identifier)) || (x.SocialSecurityNumber != null && x.SocialSecurityNumber.Contains(identifier)));
        if (!string.IsNullOrWhiteSpace(familyName))
            query = query.Where(x => x.LastName.Contains(familyName));
        if (!string.IsNullOrWhiteSpace(givenName))
            query = query.Where(x => x.FirstName.Contains(givenName));
        if (birthdate.HasValue)
            query = query.Where(x => x.DateOfBirth == birthdate.Value);

        List<PatientReadModel> list = await query
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Take(Math.Max(1, Math.Min(limit, 1000)))
            .ToListAsync(cancellationToken);
        return list.Select(ToDtoNonNull).ToList();
    }

    private static PatientReadDto? ToDto(PatientReadModel? p) =>
        p is null
            ? null
            : ToDtoNonNull(p);

    private static PatientReadDto ToDtoNonNull(PatientReadModel p) =>
        new(p.Id, p.MedicalRecordNumber, p.FirstName, p.LastName, p.DateOfBirth, p.Gender);
}
