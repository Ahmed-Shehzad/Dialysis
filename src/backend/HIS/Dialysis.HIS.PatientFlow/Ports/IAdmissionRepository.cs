using Dialysis.HIS.PatientFlow.Domain;

namespace Dialysis.HIS.PatientFlow.Ports;

public interface IAdmissionRepository
{
    void Add(Admission admission);

    Task<Admission?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
