using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Persistence.Entities;

/// <summary>
/// Lab order status from HL7 ORU OBR segment. Phase 1.2.3.
/// OBR-25: IP=In Progress, CM=Complete, CA=Canceled, etc.
/// </summary>
public sealed class LabOrderStatus
{
    public Ulid Id { get; set; }
    public TenantId TenantId { get; set; } = null!;
    public PatientId PatientId { get; set; } = null!;
    public string PlacerOrderNumber { get; set; } = "";
    public string FillerOrderNumber { get; set; } = "";
    public string? ServiceId { get; set; }  // OBR-4, e.g. LOINC
    public string Status { get; set; } = "";  // OBR-25: IP, CM, CA, etc.
    public DateTime LastUpdatedUtc { get; set; }

    public static LabOrderStatus Create(TenantId tenantId, PatientId patientId, string placerOrder, string fillerOrder, string? serviceId, string status)
    {
        return new LabOrderStatus
        {
            Id = Ulid.NewUlid(),
            TenantId = tenantId,
            PatientId = patientId,
            PlacerOrderNumber = placerOrder,
            FillerOrderNumber = fillerOrder,
            ServiceId = serviceId,
            Status = status,
            LastUpdatedUtc = DateTime.UtcNow
        };
    }
}
