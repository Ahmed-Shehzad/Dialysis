using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.QualityMeasures;

/// <summary>Returns the patient's open quality-measure care gaps for the chart's quality card.</summary>
public sealed record GetQualityGapsQuery(Guid PatientId)
    : IQuery<IReadOnlyList<QualityGap>>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.ChartRead;
}

public sealed class GetQualityGapsQueryHandler : IQueryHandler<GetQualityGapsQuery, IReadOnlyList<QualityGap>>
{
    private readonly IQualityMeasureEvaluator _evaluator;
    public GetQualityGapsQueryHandler(IQualityMeasureEvaluator evaluator) => _evaluator = evaluator;

    public Task<IReadOnlyList<QualityGap>> HandleAsync(GetQualityGapsQuery request, CancellationToken cancellationToken) =>
        _evaluator.EvaluateAsync(request.PatientId, cancellationToken);
}
