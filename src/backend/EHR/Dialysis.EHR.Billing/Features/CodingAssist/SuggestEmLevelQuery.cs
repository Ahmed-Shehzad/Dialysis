using Dialysis.CQRS.Queries;
using Dialysis.EHR.Billing.Coding;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Billing.Features.CodingAssist;

/// <summary>
/// Suggests an E/M visit level from the encounter's documented diagnoses + orders. Stateless compute —
/// the chart supplies the already-loaded codes; the suggestion is advisory (coding stays the biller's call).
/// </summary>
public sealed record SuggestEmLevelQuery(
    IReadOnlyList<string> DiagnosisIcd10,
    IReadOnlyList<string> ProcedureCpt,
    int DataReviewedCount) : IQuery<EmSuggestion?>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.BillingCodingAssist;
}

public sealed class SuggestEmLevelQueryHandler : IQueryHandler<SuggestEmLevelQuery, EmSuggestion?>
{
    private readonly IEvaluationManagementCoder _coder;
    public SuggestEmLevelQueryHandler(IEvaluationManagementCoder coder) => _coder = coder;

    public Task<EmSuggestion?> HandleAsync(SuggestEmLevelQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(_coder.Suggest(request.DiagnosisIcd10, request.ProcedureCpt, request.DataReviewedCount));
}
