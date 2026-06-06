using Dialysis.CQRS.Queries;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.OrderSets;

/// <summary>Lists active order sets (with a line summary) for the apply picker.</summary>
public sealed record ListOrderSetsQuery(int Take = 50) : IQuery<IReadOnlyList<OrderSetView>>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.OrderSetApply;
}

/// <summary>An order set projected for the picker.</summary>
public sealed record OrderSetView(
    Guid Id, string Name, string? Description, int LabLines, int MedicationLines, int ImagingLines);

public sealed class ListOrderSetsQueryHandler : IQueryHandler<ListOrderSetsQuery, IReadOnlyList<OrderSetView>>
{
    private readonly IOrderSetRepository _orderSets;
    public ListOrderSetsQueryHandler(IOrderSetRepository orderSets) => _orderSets = orderSets;

    public async Task<IReadOnlyList<OrderSetView>> HandleAsync(ListOrderSetsQuery request, CancellationToken cancellationToken)
    {
        var sets = await _orderSets.ListActiveAsync(request.Take, cancellationToken).ConfigureAwait(false);
        return [.. sets.Select(s => new OrderSetView(
            s.Id, s.Name, s.Description,
            s.Lines.Count(l => l.Kind == OrderSetLineKind.Lab),
            s.Lines.Count(l => l.Kind == OrderSetLineKind.Medication),
            s.Lines.Count(l => l.Kind == OrderSetLineKind.Imaging)))];
    }
}
