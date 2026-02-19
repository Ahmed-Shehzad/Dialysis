using System.Linq.Expressions;

using BuildingBlocks;
using BuildingBlocks.Tenancy;
using BuildingBlocks.ValueObjects;

using Dialysis.Patient.Application.Abstractions;
using Dialysis.Patient.Application.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using PatientDomain = Dialysis.Patient.Application.Domain.Patient;

namespace Dialysis.Patient.Infrastructure.Persistence;

public sealed class PatientRepository : Repository<PatientDomain>, IPatientRepository
{
    private readonly PatientDbContext _db;
    private readonly ITenantContext _tenant;

    public PatientRepository(PatientDbContext db, ITenantContext tenant) : base(db)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PatientDomain?> GetByMrnAsync(MedicalRecordNumber mrn, CancellationToken cancellationToken = default)
    {
        return await _db.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == _tenant.TenantId && p.MedicalRecordNumber == mrn, cancellationToken);
    }

    public async Task<PatientDomain?> GetByPersonNumberAsync(string personNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(personNumber)) return null;
        return await _db.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == _tenant.TenantId && p.PersonNumber == personNumber, cancellationToken);
    }

    public async Task<PatientDomain?> GetBySsnAsync(string socialSecurityNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(socialSecurityNumber)) return null;
        return await _db.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == _tenant.TenantId && p.SocialSecurityNumber == socialSecurityNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<PatientDomain>> SearchByNameAsync(Person name, CancellationToken cancellationToken = default)
    {
        return await _db.Patients
            .AsNoTracking()
            .Where(p => p.TenantId == _tenant.TenantId && p.Name.FirstName == name.FirstName && p.Name.LastName == name.LastName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PatientDomain>> SearchByLastNameAsync(string lastName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lastName)) return [];
        return await _db.Patients
            .AsNoTracking()
            .Where(p => p.TenantId == _tenant.TenantId && p.Name.LastName == lastName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PatientDomain>> GetAllForTenantAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await _db.Patients
            .AsNoTracking()
            .Where(p => p.TenantId == _tenant.TenantId)
            .OrderBy(p => p.MedicalRecordNumber.Value)
            .Take(Math.Max(1, Math.Min(limit, 10_000)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PatientDomain>> SearchForFhirAsync(string? identifier, string? familyName, string? givenName, DateOnly? birthdate, int limit, CancellationToken cancellationToken = default)
    {
        IQueryable<PatientDomain> query = _db.Patients
            .AsNoTracking()
            .Where(p => p.TenantId == _tenant.TenantId);

        if (!string.IsNullOrWhiteSpace(identifier))
            query = query.Where(p => p.MedicalRecordNumber.Value == identifier);
        if (!string.IsNullOrWhiteSpace(familyName))
            query = query.Where(p => p.Name.LastName == familyName);
        if (!string.IsNullOrWhiteSpace(givenName))
            query = query.Where(p => p.Name.FirstName == givenName);
        if (birthdate.HasValue)
            query = query.Where(p => p.DateOfBirth == birthdate.Value);

        return await query
            .OrderBy(p => p.MedicalRecordNumber.Value)
            .Take(Math.Max(1, Math.Min(limit, 10_000)))
            .ToListAsync(cancellationToken);
    }

    public async override Task AddAsync(PatientDomain patient, CancellationToken cancellationToken = default) => _ = await _db.Patients.AddAsync(patient, cancellationToken);
    public async override Task AddAsync(IEnumerable<PatientDomain> entities, CancellationToken cancellationToken = default) => await _db.Patients.AddRangeAsync(entities, cancellationToken);

    public async override Task<IReadOnlyList<PatientDomain>> GetManyAsync(Expression<Func<PatientDomain, bool>> expression, Expression<Func<PatientDomain, object>>? orderByExpression = null, bool orderByDescending = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<PatientDomain> query = _db.Patients.AsNoTracking().Where(expression);

        if (orderByExpression != null) query = orderByDescending ? query.OrderByDescending(orderByExpression) : query.OrderBy(orderByExpression);

        return await query.ToListAsync(cancellationToken);
    }

    public async override Task<PatientDomain?> GetAsync(Expression<Func<PatientDomain, bool>> expression, CancellationToken cancellationToken = default) => await _db.Patients.FirstOrDefaultAsync(expression, cancellationToken);
    public override void Update(PatientDomain entity) => _db.Update(entity);
    public override void Update(IEnumerable<PatientDomain> entities) => _db.UpdateRange(entities);
    public override void Delete(PatientDomain entity) => _db.Remove(entity);
    public override void Delete(IEnumerable<PatientDomain> entities) => _db.RemoveRange(entities);
}
