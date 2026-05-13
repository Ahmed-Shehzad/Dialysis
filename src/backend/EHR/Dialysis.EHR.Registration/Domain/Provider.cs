using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.Registration.Domain;

public enum ProviderKind
{
    Physician = 1,
    NursePractitioner = 2,
    PhysicianAssistant = 3,
    RegisteredNurse = 4,
    Pharmacist = 5,
    Therapist = 6,
    Other = 99,
}

/// <summary>Clinician/staff member. NPI is the unique federal identifier in the US (HIPAA-required).</summary>
public sealed class Provider : AggregateRoot<Guid>
{
    private Provider()
    {
    }

    public Provider(Guid id) : base(id)
    {
    }

    public string NationalProviderIdentifier { get; private set; } = string.Empty;

    public HumanName Name { get; private set; } = default!;

    public ProviderKind Kind { get; private set; }

    public string? SpecialtyCode { get; private set; }

    public string? LicenseNumber { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static Provider Register(
        Guid id,
        string nationalProviderIdentifier,
        HumanName name,
        ProviderKind kind,
        string? specialtyCode = null,
        string? licenseNumber = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nationalProviderIdentifier);
        if (nationalProviderIdentifier.Trim().Length != 10)
            throw new ArgumentException("NPI must be 10 digits.", nameof(nationalProviderIdentifier));
        ArgumentNullException.ThrowIfNull(name);

        return new Provider(id)
        {
            NationalProviderIdentifier = nationalProviderIdentifier.Trim(),
            Name = name,
            Kind = kind,
            SpecialtyCode = specialtyCode?.Trim(),
            LicenseNumber = licenseNumber?.Trim(),
        };
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;
        IsActive = false;
    }
}
