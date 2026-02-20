using BuildingBlocks;
using BuildingBlocks.ValueObjects;

using Dialysis.Prescription.Application.Domain.ValueObjects;

namespace Dialysis.Prescription.Application.Domain;

/// <summary>
/// Hemodialysis prescription aggregate parsed from RSP^K22.
/// </summary>
public sealed class Prescription : AggregateRoot
{
#pragma warning disable IDE0044, S2933 // Backing field for EF; must be writable for materialization
    private List<ProfileSetting> _settings = [];
#pragma warning restore IDE0044, S2933

    public TenantId TenantId { get; private set; }
    public OrderId OrderId { get; private set; }
    public MedicalRecordNumber PatientMrn { get; private set; }
    public string? Modality { get; private set; }
    public string? OrderingProvider { get; private set; }
    public string? CallbackPhone { get; private set; }
    public DateTimeOffset? ReceivedAt { get; private set; }
    public IReadOnlyCollection<ProfileSetting> Settings => _settings.AsReadOnly();

    private Prescription() { }

    public static Prescription Create(OrderId orderId, MedicalRecordNumber patientMrn, string? modality = null, string? orderingProvider = null, string? callbackPhone = null, string? tenantId = null)
    {
        return new Prescription
        {
            TenantId = string.IsNullOrWhiteSpace(tenantId) ? TenantId.Default : new TenantId(tenantId),
            OrderId = orderId,
            PatientMrn = patientMrn,
            Modality = modality,
            OrderingProvider = orderingProvider,
            CallbackPhone = callbackPhone,
            ReceivedAt = DateTimeOffset.UtcNow
        };
    }

    public void AddSetting(ProfileSetting setting)
    {
        ArgumentNullException.ThrowIfNull(setting);
        if (string.IsNullOrWhiteSpace(setting.Code))
            throw new ArgumentException("Setting code is required.", nameof(setting));
        if (_settings.Exists(s => s.Code == setting.Code && s.SubId == setting.SubId))
            throw new InvalidOperationException($"Duplicate setting: code '{setting.Code}', subId '{setting.SubId}'.");
        _settings.Add(setting);
    }
}
