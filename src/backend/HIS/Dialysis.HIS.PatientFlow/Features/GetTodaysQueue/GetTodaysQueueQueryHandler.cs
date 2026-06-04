using Dialysis.CQRS.Queries;
using Dialysis.HIS.PatientFlow.Domain;
using Dialysis.HIS.PatientFlow.Ports;

namespace Dialysis.HIS.PatientFlow.Features.GetTodaysQueue;

public sealed class GetTodaysQueueQueryHandler : IQueryHandler<GetTodaysQueueQuery, IReadOnlyList<PatientQueueEntryDto>>
{
    private readonly IPatientQueueRepository _repository;
    public GetTodaysQueueQueryHandler(IPatientQueueRepository repository) => _repository = repository;
    public Task<IReadOnlyList<PatientQueueEntryDto>> HandleAsync(
        GetTodaysQueueQuery _,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var entries = _repository.ListForToday(today)
            .Select(e => new PatientQueueEntryDto(
                e.Id,
                e.PatientId,
                e.PatientName,
                e.Mrn,
                e.ScheduledForUtc,
                ToWireStatus(e.Status),
                e.Chair,
                e.EligibilityVerified))
            .ToArray();
        return Task.FromResult<IReadOnlyList<PatientQueueEntryDto>>(entries);
    }

    /// <summary>Maps the enum to the lower-kebab strings the SPA's union type expects.</summary>
    private static string ToWireStatus(QueueStatus status) => status switch
    {
        QueueStatus.Expected => "expected",
        QueueStatus.Waiting => "waiting",
        QueueStatus.InTreatment => "in-treatment",
        _ => throw new InvalidOperationException($"Unknown queue status {status}."),
    };
}
