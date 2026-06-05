using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.EHR.Contracts.Integration;

namespace Dialysis.EHR.ClinicalNotes.Domain;

/// <summary>Lifecycle of an imaging order.</summary>
public enum ImagingOrderStatus
{
    Ordered = 1,
    Scheduled = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
}

/// <summary>
/// A radiology / imaging order placed in EHR. The imaging modality (PACS/RIS, reached through
/// SmartConnect DICOM) fulfils it and STOWs the study back; the study is correlated to this order by
/// <see cref="AccessionNumber"/> and recorded as <see cref="StudyInstanceUid"/> once available.
/// </summary>
public sealed class ImagingOrder : AggregateRoot<Guid>
{
    private ImagingOrder()
    {
    }

    public ImagingOrder(Guid id) : base(id)
    {
    }

    public Guid PatientId { get; private set; }
    public Guid EncounterId { get; private set; }
    public Guid OrderingProviderId { get; private set; }

    /// <summary>Stable correlation id sent to the modality and matched on the returned study.</summary>
    public string AccessionNumber { get; private set; } = string.Empty;

    /// <summary>DICOM modality code (HL7 table 0203 / DICOM: <c>US</c>, <c>CT</c>, <c>MR</c>, <c>CR</c>, <c>XA</c>…).</summary>
    public string ModalityCode { get; private set; } = string.Empty;

    /// <summary>Coded body site / procedure (free SNOMED/local code).</summary>
    public string BodySiteCode { get; private set; } = string.Empty;

    public string? ReasonText { get; private set; }

    public ImagingOrderStatus Status { get; private set; }

    /// <summary>Set once the fulfilled study is linked back to the order.</summary>
    public string? StudyInstanceUid { get; private set; }

    public string? CancellationReasonCode { get; private set; }

    /// <summary>Places a new imaging order and raises <see cref="ImagingOrderPlacedIntegrationEvent"/>.</summary>
    public static ImagingOrder Order(
        Guid id,
        Guid patientId,
        Guid encounterId,
        Guid orderingProviderId,
        string modalityCode,
        string bodySiteCode,
        string? reasonText)
    {
        if (patientId == Guid.Empty)
            throw new ArgumentException("Patient required.", nameof(patientId));
        if (encounterId == Guid.Empty)
            throw new ArgumentException("Encounter required.", nameof(encounterId));
        if (orderingProviderId == Guid.Empty)
            throw new ArgumentException("Ordering provider required.", nameof(orderingProviderId));
        ArgumentException.ThrowIfNullOrWhiteSpace(modalityCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(bodySiteCode);

        var order = new ImagingOrder(id)
        {
            PatientId = patientId,
            EncounterId = encounterId,
            OrderingProviderId = orderingProviderId,
            ModalityCode = modalityCode.Trim().ToUpperInvariant(),
            BodySiteCode = bodySiteCode.Trim(),
            ReasonText = string.IsNullOrWhiteSpace(reasonText) ? null : reasonText.Trim(),
            AccessionNumber = "IMG-" + id.ToString("N")[..12].ToUpperInvariant(),
            Status = ImagingOrderStatus.Ordered,
        };

        order.RaiseIntegrationEvent(new ImagingOrderPlacedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            ImagingOrderId: id,
            PatientId: patientId,
            EncounterId: encounterId,
            OrderingProviderId: orderingProviderId,
            AccessionNumber: order.AccessionNumber,
            ModalityCode: order.ModalityCode,
            BodySiteCode: order.BodySiteCode,
            ReasonText: order.ReasonText));

        return order;
    }

    /// <summary>Links the fulfilled DICOM study to the order and completes it.</summary>
    public void LinkStudy(string studyInstanceUid)
    {
        if (Status == ImagingOrderStatus.Cancelled)
            throw new InvalidOperationException("Cannot link a study to a cancelled order.");
        ArgumentException.ThrowIfNullOrWhiteSpace(studyInstanceUid);

        StudyInstanceUid = studyInstanceUid.Trim();
        Status = ImagingOrderStatus.Completed;
    }

    /// <summary>Marks the order scheduled / in-progress as the modality reports back.</summary>
    public void MarkScheduled() => Advance(ImagingOrderStatus.Scheduled);

    public void MarkInProgress() => Advance(ImagingOrderStatus.InProgress);

    public void Cancel(string reasonCode)
    {
        if (Status is ImagingOrderStatus.Cancelled or ImagingOrderStatus.Completed)
            throw new InvalidOperationException($"Cannot cancel an imaging order in status {Status}.");
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);

        Status = ImagingOrderStatus.Cancelled;
        CancellationReasonCode = reasonCode.Trim();
    }

    private void Advance(ImagingOrderStatus next)
    {
        if (Status is ImagingOrderStatus.Cancelled or ImagingOrderStatus.Completed)
            throw new InvalidOperationException($"Cannot move an imaging order from {Status}.");
        Status = next;
    }
}
