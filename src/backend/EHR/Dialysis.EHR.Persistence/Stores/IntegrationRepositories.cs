using Dialysis.EHR.Integration.Domain;
using Dialysis.EHR.Integration.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Stores;

public sealed class PharmacyTransmissionRepository : IPharmacyTransmissionRepository
{
    private readonly EhrDbContext _db;
    public PharmacyTransmissionRepository(EhrDbContext db) => _db = db;
    public Task<PharmacyTransmission?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.PharmacyTransmissions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<PharmacyTransmission?> FindByExternalControlNumberAsync(string controlNumber, CancellationToken cancellationToken = default) =>
        _db.PharmacyTransmissions.FirstOrDefaultAsync(t => t.ExternalControlNumber == controlNumber, cancellationToken);

    public void Add(PharmacyTransmission transmission) => _db.PharmacyTransmissions.Add(transmission);
}

public sealed class LabTransmissionRepository : ILabTransmissionRepository
{
    private readonly EhrDbContext _db;
    public LabTransmissionRepository(EhrDbContext db) => _db = db;
    public Task<LabTransmission?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.LabTransmissions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<LabTransmission?> FindByExternalControlNumberAsync(string controlNumber, CancellationToken cancellationToken = default) =>
        _db.LabTransmissions.FirstOrDefaultAsync(t => t.ExternalControlNumber == controlNumber, cancellationToken);

    public void Add(LabTransmission transmission) => _db.LabTransmissions.Add(transmission);
}

public sealed class InsurerTransmissionRepository : IInsurerTransmissionRepository
{
    private readonly EhrDbContext _db;
    public InsurerTransmissionRepository(EhrDbContext db) => _db = db;
    public Task<InsurerTransmission?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.InsurerTransmissions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<InsurerTransmission?> FindByExternalControlNumberAsync(string controlNumber, CancellationToken cancellationToken = default) =>
        _db.InsurerTransmissions.FirstOrDefaultAsync(t => t.ExternalControlNumber == controlNumber, cancellationToken);

    public void Add(InsurerTransmission transmission) => _db.InsurerTransmissions.Add(transmission);
}
