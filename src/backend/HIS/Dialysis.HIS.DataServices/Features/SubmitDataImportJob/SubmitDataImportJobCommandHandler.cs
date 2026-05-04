using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.DataServices.Domain;
using Dialysis.HIS.DataServices.Ports;

namespace Dialysis.HIS.DataServices.Features.SubmitDataImportJob;

public sealed class SubmitDataImportJobCommandHandler(IDataImportJobRepository jobs, IUnitOfWork unitOfWork)
    : ICommandHandler<SubmitDataImportJobCommand, Guid>
{
    public async Task<Guid> Handle(SubmitDataImportJobCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        jobs.Add(new DataImportJob
        {
            Id = id,
            SourceDescription = request.SourceDescription.Trim(),
            SubmittedAtUtc = DateTime.UtcNow,
            StatusCode = "Queued",
            ValidationSummary = "Validated at accept; queued for downstream ETL.",
        });
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
