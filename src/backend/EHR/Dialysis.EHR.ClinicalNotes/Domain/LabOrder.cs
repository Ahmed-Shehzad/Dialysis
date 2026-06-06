using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.EHR.Contracts.Integration;

namespace Dialysis.EHR.ClinicalNotes.Domain;

public enum LabOrderStatus
{
    Active = 1,
    Completed = 2,
    Cancelled = 3,
    InTransit = 4,
}

public sealed class LabOrder : AggregateRoot<Guid>
{
    private readonly List<string> _loincPanelCodes = new();

    private LabOrder()
    {
    }

    public LabOrder(Guid id) : base(id)
    {
    }

    public Guid PatientId { get; private set; }

    public Guid EncounterId { get; private set; }

    public Guid OrderingProviderId { get; private set; }

    public string LabFacilityCode { get; private set; } = string.Empty;

    public string TransmissionFormat { get; private set; } = string.Empty;

    public LabOrderStatus Status { get; private set; }

    public IReadOnlyCollection<string> LoincPanelCodes => _loincPanelCodes;

    public string? CancellationReasonCode { get; private set; }

    /// <summary>Clinician's reason for overriding a blocking safety advisory at order time; else null.</summary>
    public string? OverrideReason { get; private set; }

    /// <summary>Identity that overrode the blocking advisory; else null.</summary>
    public string? OverriddenBy { get; private set; }

    public static LabOrder Order(
        Guid id,
        Guid patientId,
        Guid encounterId,
        Guid orderingProviderId,
        string labFacilityCode,
        IReadOnlyList<string> loincPanelCodes,
        string transmissionFormat,
        string? overrideReason = null,
        string? overriddenBy = null)
    {
        if (patientId == Guid.Empty)
            throw new ArgumentException("Patient required.", nameof(patientId));
        if (encounterId == Guid.Empty)
            throw new ArgumentException("Encounter required.", nameof(encounterId));
        if (orderingProviderId == Guid.Empty)
            throw new ArgumentException("Ordering provider required.", nameof(orderingProviderId));
        ArgumentException.ThrowIfNullOrWhiteSpace(labFacilityCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(transmissionFormat);
        if (loincPanelCodes is null || loincPanelCodes.Count == 0)
            throw new ArgumentException("At least one LOINC panel/test code is required.", nameof(loincPanelCodes));

        var order = new LabOrder(id)
        {
            PatientId = patientId,
            EncounterId = encounterId,
            OrderingProviderId = orderingProviderId,
            LabFacilityCode = labFacilityCode.Trim(),
            TransmissionFormat = transmissionFormat.Trim(),
            Status = LabOrderStatus.Active,
            OverrideReason = string.IsNullOrWhiteSpace(overrideReason) ? null : overrideReason.Trim(),
            OverriddenBy = string.IsNullOrWhiteSpace(overriddenBy) ? null : overriddenBy.Trim(),
        };
        order._loincPanelCodes.AddRange(loincPanelCodes.Select(c => c.Trim()).Where(static c => !string.IsNullOrEmpty(c)));

        order.RaiseIntegrationEvent(new LabOrderPlacedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 2,
            LabOrderId: id,
            PatientId: patientId,
            EncounterId: encounterId,
            OrderingProviderId: orderingProviderId,
            LabFacilityCode: order.LabFacilityCode,
            LoincPanelCodes: [.. order._loincPanelCodes],
            TransmissionFormat: order.TransmissionFormat,
            OverrideReason: order.OverrideReason,
            OverriddenBy: order.OverriddenBy));

        return order;
    }

    public void Cancel(string reasonCode)
    {
        if (Status is LabOrderStatus.Cancelled or LabOrderStatus.Completed)
            throw new InvalidOperationException($"Cannot cancel a lab order in status {Status}.");
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);

        Status = LabOrderStatus.Cancelled;
        CancellationReasonCode = reasonCode.Trim();

        RaiseIntegrationEvent(new LabOrderCancelledIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            LabOrderId: Id,
            PatientId: PatientId,
            ReasonCode: CancellationReasonCode));
    }

    public void MarkCompleted()
    {
        if (Status == LabOrderStatus.Cancelled)
            throw new InvalidOperationException("Cannot complete a cancelled order.");
        Status = LabOrderStatus.Completed;
    }
}
