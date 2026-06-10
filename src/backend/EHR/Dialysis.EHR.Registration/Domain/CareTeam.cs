using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.Registration.Domain;

/// <summary>A provider's role on a patient's care team (distinct from their profession / <c>ProviderKind</c>).</summary>
public enum CareTeamRole
{
    PrimaryNephrologist = 1,
    AttendingPhysician = 2,
    DialysisNurse = 3,
    Dietitian = 4,
    SocialWorker = 5,
    CareCoordinator = 6,
    Other = 7,
}

/// <summary>A provider on a patient's care team.</summary>
public sealed class CareTeamMember : Entity<Guid>
{
    private CareTeamMember()
    {
    }

    internal CareTeamMember(Guid id, Guid careTeamId, Guid providerId, CareTeamRole role, bool isPrimary) : base(id)
    {
        CareTeamId = careTeamId;
        ProviderId = providerId;
        Role = role;
        IsPrimary = isPrimary;
    }

    public Guid CareTeamId { get; private set; }

    public Guid ProviderId { get; private set; }

    public CareTeamRole Role { get; private set; }

    public bool IsPrimary { get; private set; }

    internal void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;
}

/// <summary>
/// A patient's care team — the roster of providers (PCP, specialists, nurses) coordinating their care,
/// with at most one designated primary. The "less-fragmented, team-based view" foundation. One care team
/// per patient (keyed by <see cref="PatientId"/>).
/// </summary>
public sealed class CareTeam : AggregateRoot<Guid>
{
    private readonly List<CareTeamMember> _members = new();

    private CareTeam()
    {
    }

    public CareTeam(Guid id) : base(id)
    {
    }

    public Guid PatientId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<CareTeamMember> Members => _members;

    public static CareTeam Create(Guid id, Guid patientId, DateTime nowUtc)
    {
        if (patientId == Guid.Empty)
            throw new ArgumentException("Patient required.", nameof(patientId));
        return new CareTeam(id) { PatientId = patientId, CreatedAtUtc = nowUtc };
    }

    public CareTeamMember AddMember(Guid memberId, Guid providerId, CareTeamRole role, bool isPrimary)
    {
        if (providerId == Guid.Empty)
            throw new ArgumentException("Provider required.", nameof(providerId));
        if (_members.Exists(m => m.ProviderId == providerId))
            throw new InvalidOperationException("Provider is already on this care team.");

        if (isPrimary)
            DemotePrimaries();

        var member = new CareTeamMember(memberId, Id, providerId, role, isPrimary);
        _members.Add(member);
        return member;
    }

    public void RemoveMember(Guid providerId)
    {
        var member = _members.Find(m => m.ProviderId == providerId)
            ?? throw new InvalidOperationException("Provider is not on this care team.");
        _members.Remove(member);
    }

    public void SetPrimary(Guid providerId)
    {
        var target = _members.Find(m => m.ProviderId == providerId)
            ?? throw new InvalidOperationException("Provider is not on this care team.");
        DemotePrimaries();
        target.SetPrimary(true);
    }

    private void DemotePrimaries()
    {
        foreach (var m in _members.Where(m => m.IsPrimary))
            m.SetPrimary(false);
    }
}
