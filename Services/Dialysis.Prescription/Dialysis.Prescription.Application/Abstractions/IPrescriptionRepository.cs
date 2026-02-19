using BuildingBlocks.Abstractions;
using BuildingBlocks.ValueObjects;

using Dialysis.Prescription.Application.Domain.ValueObjects;

using PrescriptionEntity = Dialysis.Prescription.Application.Domain.Prescription;

namespace Dialysis.Prescription.Application.Abstractions;

public interface IPrescriptionRepository : IRepository<PrescriptionEntity>
{
    Task<PrescriptionEntity?> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default);
    /// <summary>Used by ProcessQbpD01CommandHandler to build RSP^K22 from full entity.</summary>
    Task<PrescriptionEntity?> GetLatestByMrnAsync(MedicalRecordNumber mrn, CancellationToken cancellationToken = default);
}
