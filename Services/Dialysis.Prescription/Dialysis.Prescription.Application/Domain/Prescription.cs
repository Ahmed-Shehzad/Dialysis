using BuildingBlocks;
using BuildingBlocks.Tenancy;
using BuildingBlocks.ValueObjects;

namespace Dialysis.Prescription.Application.Domain;

/// <summary>
/// Hemodialysis prescription aggregate parsed from RSP^K22.
/// </summary>
public sealed class Prescription : AggregateRoot
{
    private readonly List<ProfileSetting> _settings = [];

    public string TenantId { get; private set; } = TenantContext.DefaultTenantId;
    public string OrderId { get; private set; } = string.Empty;
    public MedicalRecordNumber PatientMrn { get; private set; }
    public string? Modality { get; private set; }
    public string? OrderingProvider { get; private set; }
    public string? CallbackPhone { get; private set; }
    public DateTimeOffset? ReceivedAt { get; private set; }
    public IReadOnlyCollection<ProfileSetting> Settings => _settings.AsReadOnly();

    /// <summary>
    /// Used by EF Core for persistence. Value converter in Infrastructure handles JSON serialization.
    /// </summary>
    internal List<ProfileSetting> SettingsForPersistence
    {
        get => _settings;
        set
        {
            _settings.Clear();
            if (value is not null)
                _settings.AddRange(value);
        }
    }

    private Prescription() { }

    public static Prescription Create(string orderId, MedicalRecordNumber patientMrn, string? modality = null, string? orderingProvider = null, string? callbackPhone = null, string? tenantId = null)
    {
        return new Prescription
        {
            TenantId = string.IsNullOrWhiteSpace(tenantId) ? TenantContext.DefaultTenantId : tenantId,
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
