using Dialysis.CQRS.Queries;
using Dialysis.PDMS.TreatmentSessions.Projections;

namespace Dialysis.PDMS.TreatmentSessions.Features.ListChairAssignments;

public sealed class ListChairAssignmentsQueryHandler : IQueryHandler<ListChairAssignmentsQuery, IReadOnlyList<ChairAssignmentDto>>
{
    private readonly ChairOccupancyProjection _projection;
    public ListChairAssignmentsQueryHandler(ChairOccupancyProjection projection) => _projection = projection;
    public Task<IReadOnlyList<ChairAssignmentDto>> HandleAsync(
        ListChairAssignmentsQuery _,
        CancellationToken cancellationToken)
    {
        var snapshot = _projection.List();
        IReadOnlyList<ChairAssignmentDto> result =
            [.. snapshot.Select(a => new ChairAssignmentDto(a.Chair, a.PatientId, a.PlacedAtUtc))];
        return Task.FromResult(result);
    }
}
