using Dialysis.CQRS.Commands;
using Dialysis.Lab.Contracts;
using Dialysis.Lab.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Lab.Orders.Features.PlaceLabOrder;

/// <summary>
/// Places a lab order for a patient. Order-entry originates in the EHR chart (routed through the EHR
/// BFF's <c>/ehr/api/_x/lab</c> aggregation); the handler persists the order and enqueues
/// <c>LabOrderPlacedIntegrationEvent</c> for SmartConnect to transmit to the LIS.
/// </summary>
public sealed record PlaceLabOrderCommand : ICommand<Guid>, IPermissionedCommand
{
    /// <summary>Places a lab order for a patient.</summary>
    public PlaceLabOrderCommand(Guid PatientId,
        IReadOnlyList<LabTestRequestContract> Tests,
        LabOrderPriority Priority,
        string? Specimen,
        string PlacedBy)
    {
        this.PatientId = PatientId;
        this.Tests = Tests;
        this.Priority = Priority;
        this.Specimen = Specimen;
        this.PlacedBy = PlacedBy;
    }
    public string RequiredPermission => LabPermissions.OrderPlace;
    public Guid PatientId { get; init; }
    public IReadOnlyList<LabTestRequestContract> Tests { get; init; }
    public LabOrderPriority Priority { get; init; }
    public string? Specimen { get; init; }
    public string PlacedBy { get; init; }
    public void Deconstruct(out Guid PatientId, out IReadOnlyList<LabTestRequestContract> Tests, out LabOrderPriority Priority, out string? Specimen, out string PlacedBy)
    {
        PatientId = this.PatientId;
        Tests = this.Tests;
        Priority = this.Priority;
        Specimen = this.Specimen;
        PlacedBy = this.PlacedBy;
    }
}
