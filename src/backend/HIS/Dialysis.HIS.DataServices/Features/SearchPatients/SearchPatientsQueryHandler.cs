using Dialysis.CQRS.Queries;
using Dialysis.HIS.DataServices.Ports;

namespace Dialysis.HIS.DataServices.Features.SearchPatients;

public sealed class SearchPatientsQueryHandler(IPatientSearchReadModel readModel)
    : IQueryHandler<SearchPatientsQuery, IReadOnlyList<PatientSearchResultDto>>
{
    public Task<IReadOnlyList<PatientSearchResultDto>> Handle(SearchPatientsQuery request, CancellationToken cancellationToken) =>
        readModel.SearchAsync(request.MrnContains, cancellationToken);
}
