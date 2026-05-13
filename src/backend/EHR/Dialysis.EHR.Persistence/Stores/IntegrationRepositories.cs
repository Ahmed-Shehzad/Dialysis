using Dialysis.EHR.Integration.Domain;
using Dialysis.EHR.Integration.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Stores;

public sealed class PharmacyTransmissionRepository(EhrDbContext db) : IPharmacyTransmissionRepository
{
    public Task<PharmacyTransmission?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.PharmacyTransmissions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<PharmacyTransmission?> FindByExternalControlNumberAsync(string controlNumber, CancellationToken cancellationToken = default) =>
        db.PharmacyTransmissions.FirstOrDefaultAsync(t => t.ExternalControlNumber == controlNumber, cancellationToken);

    public void Add(PharmacyTransmission transmission) => db.PharmacyTransmissions.Add(transmission);
}

public sealed class LabTransmissionRepository(EhrDbContext db) : ILabTransmissionRepository
{
    public Task<LabTransmission?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.LabTransmissions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<LabTransmission?> FindByExternalControlNumberAsync(string controlNumber, CancellationToken cancellationToken = default) =>
        db.LabTransmissions.FirstOrDefaultAsync(t => t.ExternalControlNumber == controlNumber, cancellationToken);

    public void Add(LabTransmission transmission) => db.LabTransmissions.Add(transmission);
}

public sealed class InsurerTransmissionRepository(EhrDbContext db) : IInsurerTransmissionRepository
{
    public Task<InsurerTransmission?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.InsurerTransmissions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<InsurerTransmission?> FindByExternalControlNumberAsync(string controlNumber, CancellationToken cancellationToken = default) =>
        db.InsurerTransmissions.FirstOrDefaultAsync(t => t.ExternalControlNumber == controlNumber, cancellationToken);

    public void Add(InsurerTransmission transmission) => db.InsurerTransmissions.Add(transmission);
}
