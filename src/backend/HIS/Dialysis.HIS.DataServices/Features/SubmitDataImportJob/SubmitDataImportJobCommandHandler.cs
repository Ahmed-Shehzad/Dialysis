using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.DataServices.Domain;
using Dialysis.HIS.DataServices.Ports;

namespace Dialysis.HIS.DataServices.Features.SubmitDataImportJob;

public sealed class SubmitDataImportJobCommandHandler : ICommandHandler<SubmitDataImportJobCommand, Guid>
{
    private readonly IDataImportJobRepository _jobs;
    private readonly IUnitOfWork _unitOfWork;
    public SubmitDataImportJobCommandHandler(IDataImportJobRepository jobs, IUnitOfWork unitOfWork)
    {
        _jobs = jobs;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(SubmitDataImportJobCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        _jobs.Add(new DataImportJob
        {
            Id = id,
            SourceDescription = request.SourceDescription.Trim(),
            SubmittedAtUtc = DateTime.UtcNow,
            StatusCode = "Queued",
            ValidationSummary = "Validated at accept; queued for downstream ETL.",
        });
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
