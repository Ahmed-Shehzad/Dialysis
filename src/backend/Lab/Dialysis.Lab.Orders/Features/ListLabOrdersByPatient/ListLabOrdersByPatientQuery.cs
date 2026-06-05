using Dialysis.CQRS.Queries;
using Dialysis.Lab.Contracts;
using Dialysis.Lab.Contracts.Security;
using Dialysis.Lab.Orders.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Lab.Orders.Features.ListLabOrdersByPatient;

public sealed record ListLabOrdersByPatientQuery : IQuery<IReadOnlyList<LabOrderSummaryDto>>, IPermissionedCommand
{
    public ListLabOrdersByPatientQuery(Guid PatientId, int Take = 50)
    {
        this.PatientId = PatientId;
        this.Take = Take;
    }
    public string RequiredPermission => LabPermissions.OrderRead;
    public Guid PatientId { get; init; }
    public int Take { get; init; }
    public void Deconstruct(out Guid PatientId, out int Take)
    {
        PatientId = this.PatientId;
        Take = this.Take;
    }
}

/// <summary>Compact order projection for the chart's lab list (no observation lines).</summary>
public sealed record LabOrderSummaryDto(
    Guid Id,
    string PlacerOrderNumber,
    LabOrderPriority Priority,
    LabOrderStatus Status,
    int TestCount,
    DateTime PlacedAtUtc,
    DateTime? ResultedAtUtc);

public sealed class ListLabOrdersByPatientQueryHandler
    : IQueryHandler<ListLabOrdersByPatientQuery, IReadOnlyList<LabOrderSummaryDto>>
{
    private readonly ILabOrderRepository _orders;
    public ListLabOrdersByPatientQueryHandler(ILabOrderRepository orders) => _orders = orders;

    public async Task<IReadOnlyList<LabOrderSummaryDto>> HandleAsync(
        ListLabOrdersByPatientQuery request, CancellationToken cancellationToken)
    {
        var take = request.Take is > 0 and <= 200 ? request.Take : 50;
        var orders = await _orders.ListByPatientAsync(request.PatientId, take, cancellationToken).ConfigureAwait(false);
        return [.. orders.Select(o => new LabOrderSummaryDto(
            o.Id, o.PlacerOrderNumber, o.Priority, o.Status, o.Tests.Count, o.PlacedAtUtc, o.ResultedAtUtc))];
    }
}
