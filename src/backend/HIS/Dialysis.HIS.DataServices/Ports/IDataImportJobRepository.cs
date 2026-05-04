using Dialysis.HIS.DataServices.Domain;

namespace Dialysis.HIS.DataServices.Ports;

public interface IDataImportJobRepository
{
    void Add(DataImportJob job);

    Task<DataImportJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
