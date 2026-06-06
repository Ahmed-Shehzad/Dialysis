using Dialysis.CQRS.Queries;

namespace Dialysis.HIE.Inbound.Insights.Features;

public sealed class GetPatientInsightsQueryHandler : IQueryHandler<GetPatientInsightsQuery, PatientInsightsSummary>
{
    private readonly ExternalPatientInsightsBuilder _builder;
    public GetPatientInsightsQueryHandler(ExternalPatientInsightsBuilder builder) => _builder = builder;

    public Task<PatientInsightsSummary> HandleAsync(GetPatientInsightsQuery request, CancellationToken cancellationToken) =>
        _builder.BuildAsync(request.PatientReference, request.Scan, request.RecentTake, cancellationToken);
}
