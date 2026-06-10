using Dialysis.CQRS.Queries;
using Dialysis.Lab.Contracts;
using Dialysis.Lab.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Lab.Orders.Features.GetLabOrderById;

public sealed record GetLabOrderByIdQuery : IQuery<LabOrderDto?>, IPermissionedCommand
{
    public GetLabOrderByIdQuery(Guid Id) => this.Id = Id;
    public string RequiredPermission => LabPermissions.OrderRead;
    public Guid Id { get; init; }
    public void Deconstruct(out Guid id) => id = Id;
}

/// <summary>Full order projection incl. requested tests and any returned observations.</summary>
public sealed record LabOrderDto(
    Guid Id,
    Guid PatientId,
    string PlacerOrderNumber,
    string? FillerOrderNumber,
    LabOrderPriority Priority,
    LabOrderStatus Status,
    string? Specimen,
    string PlacedBy,
    DateTime PlacedAtUtc,
    DateTime? ResultedAtUtc,
    IReadOnlyList<LabTestRequestContract> Tests,
    IReadOnlyList<LabObservationContract> Results);
