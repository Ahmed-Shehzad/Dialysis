namespace Dialysis.Prescription.Infrastructure.ReadModels;

/// <summary>
/// Read-only projection of Prescription for query operations. Maps to the Prescriptions table.
/// </summary>
public sealed class PrescriptionReadModel
{
    public string Id { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string OrderId { get; init; } = string.Empty;
    public string PatientMrn { get; init; } = string.Empty;
    public string? Modality { get; init; }
    public string? OrderingProvider { get; init; }
    public string? CallbackPhone { get; init; }
    public DateTimeOffset? ReceivedAt { get; init; }
    /// <summary>JSON-serialized profile settings. Use PrescriptionSettingsSerializer.FromJson to deserialize.</summary>
    public string? SettingsJson { get; init; }
}
