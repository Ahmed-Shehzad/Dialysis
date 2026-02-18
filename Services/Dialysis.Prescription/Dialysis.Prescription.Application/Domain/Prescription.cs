using BuildingBlocks;
using BuildingBlocks.Tenancy;
using BuildingBlocks.ValueObjects;

using Dialysis.Prescription.Application.Persistence;

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
    /// JSON-serialized settings for EF persistence. Backed by <see cref="_settings"/>;
    /// EF never inspects nested types (ProfileSetting, ProfileDescriptor).
    /// </summary>
    internal string? SettingsJson
    {
        get => _settings.Count == 0 ? null : PrescriptionSettingsSerializer.ToJson(_settings);
        set
        {
            _settings.Clear();
            if (!string.IsNullOrEmpty(value))
                _settings.AddRange(PrescriptionSettingsSerializer.FromJson(value));
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

    public void AddSetting(ProfileSetting setting) => _settings.Add(setting);
}
