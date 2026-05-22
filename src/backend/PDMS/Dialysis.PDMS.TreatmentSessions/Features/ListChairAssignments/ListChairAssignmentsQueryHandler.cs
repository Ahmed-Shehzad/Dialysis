using Dialysis.CQRS.Queries;
using Dialysis.PDMS.TreatmentSessions.Projections;

namespace Dialysis.PDMS.TreatmentSessions.Features.ListChairAssignments;

public sealed class ListChairAssignmentsQueryHandler(ChairOccupancyProjection projection)
    : IQueryHandler<ListChairAssignmentsQuery, IReadOnlyList<ChairAssignmentDto>>
{
    public Task<IReadOnlyList<ChairAssignmentDto>> HandleAsync(
        ListChairAssignmentsQuery _,
        CancellationToken cancellationToken)
    {
        var snapshot = projection.List();
        IReadOnlyList<ChairAssignmentDto> result =
            [.. snapshot.Select(a => new ChairAssignmentDto(a.Chair, a.PatientId, a.PlacedAtUtc))];
        return Task.FromResult(result);
    }
}
