using BuildingBlocks.Abstractions;
using BuildingBlocks.ValueObjects;

using PrescriptionEntity = Dialysis.Prescription.Application.Domain.Prescription;

namespace Dialysis.Prescription.Application.Abstractions;

public interface IPrescriptionRepository : IRepository<PrescriptionEntity>
{
    Task<PrescriptionEntity?> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken = default);
    Task<PrescriptionEntity?> GetLatestByMrnAsync(MedicalRecordNumber mrn, CancellationToken cancellationToken = default);
}
