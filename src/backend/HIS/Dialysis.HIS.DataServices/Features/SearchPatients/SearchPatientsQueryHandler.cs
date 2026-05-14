using Dialysis.CQRS.Queries;
using Dialysis.HIS.DataServices.Ports;

namespace Dialysis.HIS.DataServices.Features.SearchPatients;

public sealed class SearchPatientsQueryHandler(IPatientSearchReadModel readModel)
    : IQueryHandler<SearchPatientsQuery, IReadOnlyList<PatientSearchRow>>
{
    public Task<IReadOnlyList<PatientSearchRow>> HandleAsync(SearchPatientsQuery request, CancellationToken cancellationToken) =>
        readModel.SearchAsync(request.Q, request.Skip, request.Take, cancellationToken);
}
