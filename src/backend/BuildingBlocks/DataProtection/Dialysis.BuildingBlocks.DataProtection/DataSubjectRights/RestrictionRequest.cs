namespace Dialysis.BuildingBlocks.DataProtection.DataSubjectRights;

/// <summary>Status of a GDPR Art. 18 restriction-of-processing request.</summary>
public enum RestrictionRequestStatus
{
    /// <summary>Filed by the subject; processing of the patient's data is restricted pending resolution.</summary>
    Active = 1,
    /// <summary>Operator lifted the restriction once the dispute that prompted it was resolved.</summary>
    Lifted = 2,
}

/// <summary>
/// Audit-trail row for one GDPR Art. 18 restriction-of-processing request. Persisted when the
/// subject files the request so a regulator can verify both the inbound claim and the operator's
/// decision to lift it. Mirrors <see cref="ErasureRequest"/> — the platform records the request
/// and its lifecycle; module-level enforcement of the restriction flag is a deferred concern.
/// </summary>
public sealed record RestrictionRequest
{
    /// <summary>
    /// Audit-trail row for one GDPR Art. 18 restriction-of-processing request. Persisted when the
    /// subject files the request so a regulator can verify both the inbound claim and the operator's
    /// decision to lift it.
    /// </summary>
    public RestrictionRequest(Guid Id,
        Guid PatientId,
        RestrictionRequestStatus Status,
        string RequestedBy,
        DateTimeOffset RequestedAtUtc,
        string? Reason,
        string? LiftedBy,
        DateTimeOffset? LiftedAtUtc,
        string? LiftReason)
    {
        this.Id = Id;
        this.PatientId = PatientId;
        this.Status = Status;
        this.RequestedBy = RequestedBy;
        this.RequestedAtUtc = RequestedAtUtc;
        this.Reason = Reason;
        this.LiftedBy = LiftedBy;
        this.LiftedAtUtc = LiftedAtUtc;
        this.LiftReason = LiftReason;
    }
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public RestrictionRequestStatus Status { get; init; }
    public string RequestedBy { get; init; }
    public DateTimeOffset RequestedAtUtc { get; init; }
    public string? Reason { get; init; }
    public string? LiftedBy { get; init; }
    public DateTimeOffset? LiftedAtUtc { get; init; }
    public string? LiftReason { get; init; }
    public void Deconstruct(out Guid Id, out Guid PatientId, out RestrictionRequestStatus Status, out string RequestedBy, out DateTimeOffset RequestedAtUtc, out string? Reason, out string? LiftedBy, out DateTimeOffset? LiftedAtUtc, out string? LiftReason)
    {
        Id = this.Id;
        PatientId = this.PatientId;
        Status = this.Status;
        RequestedBy = this.RequestedBy;
        RequestedAtUtc = this.RequestedAtUtc;
        Reason = this.Reason;
        LiftedBy = this.LiftedBy;
        LiftedAtUtc = this.LiftedAtUtc;
        LiftReason = this.LiftReason;
    }
}
