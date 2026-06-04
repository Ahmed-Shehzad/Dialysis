namespace Dialysis.HIS.PatientFlow.Features.GetTodaysQueue;

/// <summary>
/// Wire shape for one row of the receptionist's "Today" queue. The string status lets the
/// SPA stay decoupled from the C# enum; values are lower-kebab so the front-end union type
/// (<c>"expected" | "waiting" | "in-treatment"</c>) is the source of truth.
/// </summary>
public sealed record PatientQueueEntryDto
{
    /// <summary>
    /// Wire shape for one row of the receptionist's "Today" queue. The string status lets the
    /// SPA stay decoupled from the C# enum; values are lower-kebab so the front-end union type
    /// (<c>"expected" | "waiting" | "in-treatment"</c>) is the source of truth.
    /// </summary>
    public PatientQueueEntryDto(Guid Id,
        Guid PatientId,
        string PatientName,
        string Mrn,
        DateTime ScheduledForUtc,
        string Status,
        string? Chair,
        bool EligibilityVerified)
    {
        this.Id = Id;
        this.PatientId = PatientId;
        this.PatientName = PatientName;
        this.Mrn = Mrn;
        this.ScheduledForUtc = ScheduledForUtc;
        this.Status = Status;
        this.Chair = Chair;
        this.EligibilityVerified = EligibilityVerified;
    }
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public string PatientName { get; init; }
    public string Mrn { get; init; }
    public DateTime ScheduledForUtc { get; init; }
    public string Status { get; init; }
    public string? Chair { get; init; }
    public bool EligibilityVerified { get; init; }
    public void Deconstruct(out Guid Id, out Guid PatientId, out string PatientName, out string Mrn, out DateTime ScheduledForUtc, out string Status, out string? Chair, out bool EligibilityVerified)
    {
        Id = this.Id;
        PatientId = this.PatientId;
        PatientName = this.PatientName;
        Mrn = this.Mrn;
        ScheduledForUtc = this.ScheduledForUtc;
        Status = this.Status;
        Chair = this.Chair;
        EligibilityVerified = this.EligibilityVerified;
    }
}
