using BuildingBlocks;

using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Domain.Entities;

/// <summary>
/// Domain entity representing a patient. Inherits BaseEntity per DDD.
/// </summary>
public sealed class Patient : BaseEntity
{
    public TenantId TenantId { get; private set; }
    public PatientId LogicalId { get; private set; }
    public string? FamilyName { get; private set; }
    public string? GivenNames { get; private set; }
    public DateTime? BirthDate { get; private set; }

    private Patient()
    {
        TenantId = null!;
        LogicalId = null!;
    }

    public static Patient Create(TenantId tenantId, PatientId logicalId, string? familyName = null, string? givenNames = null, DateTime? birthDate = null)
    {
        return new Patient
        {
            TenantId = tenantId,
            LogicalId = logicalId,
            FamilyName = familyName,
            GivenNames = givenNames,
            BirthDate = birthDate
        };
    }

    public void Update(string? familyName, string? givenNames, DateTime? birthDate)
    {
        if (familyName is not null)
            FamilyName = familyName;
        if (givenNames is not null)
            GivenNames = givenNames;
        if (birthDate.HasValue)
            BirthDate = birthDate;
        ApplyUpdateDateTime();
    }
}
