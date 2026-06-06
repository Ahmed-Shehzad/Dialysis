using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.ClinicalDecisionSupport;

/// <summary>Returns the patient's currently-firing clinical decision-support recommendations for the chart.</summary>
public sealed record GetClinicalRecommendationsQuery(Guid PatientId)
    : IQuery<IReadOnlyList<CdsRecommendation>>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.ChartRead;
}

public sealed class GetClinicalRecommendationsQueryHandler
    : IQueryHandler<GetClinicalRecommendationsQuery, IReadOnlyList<CdsRecommendation>>
{
    private readonly IClinicalDecisionSupportEvaluator _evaluator;
    public GetClinicalRecommendationsQueryHandler(IClinicalDecisionSupportEvaluator evaluator) => _evaluator = evaluator;

    public Task<IReadOnlyList<CdsRecommendation>> HandleAsync(
        GetClinicalRecommendationsQuery request, CancellationToken cancellationToken) =>
        _evaluator.EvaluateAsync(request.PatientId, cancellationToken);
}
