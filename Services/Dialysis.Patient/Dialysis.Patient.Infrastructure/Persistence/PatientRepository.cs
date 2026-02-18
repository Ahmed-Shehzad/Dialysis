using System.Linq.Expressions;

using BuildingBlocks;
using BuildingBlocks.ValueObjects;

using Dialysis.Patient.Application.Abstractions;
using Dialysis.Patient.Application.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using PatientDomain = Dialysis.Patient.Application.Domain.Patient;

namespace Dialysis.Patient.Infrastructure.Persistence;

public sealed class PatientRepository : Repository<PatientDomain>, IPatientRepository
{
    private readonly PatientDbContext _db;

    public PatientRepository(PatientDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<PatientDomain?> GetByMrnAsync(MedicalRecordNumber mrn, CancellationToken cancellationToken = default)
    {
        return await _db.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.MedicalRecordNumber == mrn, cancellationToken);
    }

    public async Task<IReadOnlyList<PatientDomain>> SearchByNameAsync(Person name, CancellationToken cancellationToken = default)
    {
        return await _db.Patients
            .AsNoTracking()
            .Where(p => p.Name.FirstName == name.FirstName && p.Name.LastName == name.LastName)
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
