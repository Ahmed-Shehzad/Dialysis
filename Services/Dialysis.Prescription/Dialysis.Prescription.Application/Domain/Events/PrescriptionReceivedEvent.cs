using BuildingBlocks;
using BuildingBlocks.ValueObjects;

using Dialysis.Prescription.Application.Domain.ValueObjects;

namespace Dialysis.Prescription.Application.Domain.Events;

/// <summary>
/// Raised when a prescription has been fully received and is ready for persistence.
/// </summary>
public sealed record PrescriptionReceivedEvent(
    Ulid PrescriptionId,
    OrderId OrderId,
    MedicalRecordNumber PatientMrn,
    string? TenantId) : DomainEvent;
