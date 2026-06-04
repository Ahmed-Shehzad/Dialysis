using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.RaCapabilities.Domain;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.RequestAnalyticsExportJob;

public sealed class RequestAnalyticsExportJobCommandHandler : ICommandHandler<RequestAnalyticsExportJobCommand, Guid>
{
    private readonly IRaCapabilityCommandStore _store;
    private readonly IUnitOfWork _unitOfWork;
    public RequestAnalyticsExportJobCommandHandler(IRaCapabilityCommandStore store, IUnitOfWork unitOfWork)
    {
        _store = store;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(RequestAnalyticsExportJobCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        _store.AddAnalyticsExportJob(
            new RaAnalyticsExportJob
            {
                Id = id,
                PipelineCode = request.PipelineCode.Trim(),
                RequestedAtUtc = DateTime.UtcNow,
                StatusCode = "Requested",
            });
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
