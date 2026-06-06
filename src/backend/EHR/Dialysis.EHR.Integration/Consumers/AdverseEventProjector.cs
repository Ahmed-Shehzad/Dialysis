using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Integration.Ports;
using Dialysis.EHR.Integration.ReadModels;
using Dialysis.PDMS.Contracts.Integration;

namespace Dialysis.EHR.Integration.Consumers;

/// <summary>
/// Projects PDMS intradialytic adverse events into the cross-patient surveillance read model. Idempotent
/// on the source event id, so redelivery never double-counts.
/// </summary>
public sealed class AdverseEventProjector : IConsumer<IntradialyticAdverseEventIntegrationEvent>
{
    private readonly IAdverseEventRepository _events;
    private readonly IUnitOfWork _unitOfWork;

    public AdverseEventProjector(IAdverseEventRepository events, IUnitOfWork unitOfWork)
    {
        _events = events;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(ConsumeContext<IntradialyticAdverseEventIntegrationEvent> context)
    {
        var m = context.Message;
        await _events.RecordAsync(new AdverseEventRecord
        {
            Id = Guid.CreateVersion7(),
            PatientId = m.PatientId,
            SessionId = m.SessionId,
            Kind = m.EventKindCode,
            Severity = m.Severity,
            Detail = m.Notes,
            OccurredAtUtc = m.ObservedAtUtc,
            SourceEventKey = m.EventId.ToString(),
        }, context.CancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
    }
}
