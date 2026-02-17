using BuildingBlocks;

using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Domain.Aggregates;

/// <summary>
/// Dialysis session aggregate. Tracks session start/stop, access site, ultrafiltration.
/// </summary>
public sealed class Session : AggregateRoot
{
    public TenantId TenantId { get; private set; }
    public PatientId PatientId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public string? AccessSite { get; private set; }  // e.g. fistula, graft, catheter
    public string? EncounterId { get; private set; }  // optional link to EHR encounter/visit
    public decimal? UfRemovedKg { get; private set; }  // ultrafiltration removed (kg)
    public SessionStatus Status { get; private set; }

    private Session()
    {
        TenantId = null!;
        PatientId = null!;
        Status = SessionStatus.Unknown;
    }

    public static Session Start(TenantId tenantId, PatientId patientId, string? accessSite = null, string? encounterId = null)
    {
        return new Session
        {
            TenantId = tenantId,
            PatientId = patientId,
            StartedAt = DateTimeOffset.UtcNow,
            Status = SessionStatus.InProgress,
            AccessSite = accessSite,
            EncounterId = encounterId
        };
    }

    public void SetEncounter(string? encounterId)
    {
        EncounterId = encounterId;
        ApplyUpdateDateTime();
    }

    public void Complete(decimal? ufRemovedKg = null)
    {
        if (Status == SessionStatus.Completed)
            return;
        EndedAt = DateTimeOffset.UtcNow;
        Status = SessionStatus.Completed;
        if (ufRemovedKg.HasValue)
            UfRemovedKg = ufRemovedKg;
        ApplyUpdateDateTime();
    }

    public void UpdateUf(decimal ufRemovedKg)
    {
        UfRemovedKg = ufRemovedKg;
        ApplyUpdateDateTime();
    }
}

public enum SessionStatus
{
    Unknown = 0,
    InProgress = 1,
    Completed = 2
}
