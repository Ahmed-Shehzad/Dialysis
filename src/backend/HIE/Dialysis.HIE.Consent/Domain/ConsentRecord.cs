namespace Dialysis.HIE.Consent.Domain;

/// <summary>
/// Patient consent for cross-organization disclosure to a specific partner within a specific scope.
/// A record is honoured when <see cref="EffectiveFromUtc"/> is in the past, <see cref="EffectiveToUtc"/>
/// is null or in the future, and <see cref="RevokedAtUtc"/> is null.
/// </summary>
public sealed class ConsentRecord
{
    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public string PartnerId { get; private set; } = string.Empty;
    public string Scope { get; private set; } = string.Empty;
    public ConsentDirection Direction { get; private set; }
    public DateTime EffectiveFromUtc { get; private set; }
    public DateTime? EffectiveToUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    private ConsentRecord() { }

    public ConsentRecord(Guid patientId, string partnerId, string scope, ConsentDirection direction, DateTime effectiveFromUtc, DateTime? effectiveToUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partnerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        Id = Guid.NewGuid();
        PatientId = patientId;
        PartnerId = partnerId;
        Scope = scope;
        Direction = direction;
        EffectiveFromUtc = effectiveFromUtc;
        EffectiveToUtc = effectiveToUtc;
    }

    public void Revoke(DateTime atUtc) => RevokedAtUtc = atUtc;

    public bool IsActiveAt(DateTime atUtc) =>
        RevokedAtUtc is null
        && EffectiveFromUtc <= atUtc
        && (EffectiveToUtc is null || EffectiveToUtc > atUtc);
}

public enum ConsentDirection
{
    Outbound = 1,
    Inbound = 2,
    Bidirectional = 3,
}
