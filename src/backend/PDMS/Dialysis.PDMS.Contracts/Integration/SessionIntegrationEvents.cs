using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.PDMS.Contracts.Integration;

public sealed record DialysisSessionStartedIntegrationEvent : IIntegrationEvent
{
    public DialysisSessionStartedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid SessionId,
        Guid PatientId,
        DateTime StartedAtUtc,
        string DialyzerModel,
        int BloodFlowRateMlPerMin)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.SessionId = SessionId;
        this.PatientId = PatientId;
        this.StartedAtUtc = StartedAtUtc;
        this.DialyzerModel = DialyzerModel;
        this.BloodFlowRateMlPerMin = BloodFlowRateMlPerMin;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid SessionId { get; init; }
    public Guid PatientId { get; init; }
    public DateTime StartedAtUtc { get; init; }
    public string DialyzerModel { get; init; }
    public int BloodFlowRateMlPerMin { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid SessionId, out Guid PatientId, out DateTime StartedAtUtc, out string DialyzerModel, out int BloodFlowRateMlPerMin)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        SessionId = this.SessionId;
        PatientId = this.PatientId;
        StartedAtUtc = this.StartedAtUtc;
        DialyzerModel = this.DialyzerModel;
        BloodFlowRateMlPerMin = this.BloodFlowRateMlPerMin;
    }
}

public sealed record DialysisSessionCompletedIntegrationEvent : IIntegrationEvent
{
    public DialysisSessionCompletedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid SessionId,
        Guid PatientId,
        DateTime CompletedAtUtc,
        int ActualDurationMinutes,
        decimal AchievedUfVolumeLiters)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.SessionId = SessionId;
        this.PatientId = PatientId;
        this.CompletedAtUtc = CompletedAtUtc;
        this.ActualDurationMinutes = ActualDurationMinutes;
        this.AchievedUfVolumeLiters = AchievedUfVolumeLiters;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid SessionId { get; init; }
    public Guid PatientId { get; init; }
    public DateTime CompletedAtUtc { get; init; }
    public int ActualDurationMinutes { get; init; }
    public decimal AchievedUfVolumeLiters { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid SessionId, out Guid PatientId, out DateTime CompletedAtUtc, out int ActualDurationMinutes, out decimal AchievedUfVolumeLiters)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        SessionId = this.SessionId;
        PatientId = this.PatientId;
        CompletedAtUtc = this.CompletedAtUtc;
        ActualDurationMinutes = this.ActualDurationMinutes;
        AchievedUfVolumeLiters = this.AchievedUfVolumeLiters;
    }
}

public sealed record DialysisSessionAbortedIntegrationEvent : IIntegrationEvent
{
    public DialysisSessionAbortedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid SessionId,
        Guid PatientId,
        DateTime AbortedAtUtc,
        string ReasonCode)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.SessionId = SessionId;
        this.PatientId = PatientId;
        this.AbortedAtUtc = AbortedAtUtc;
        this.ReasonCode = ReasonCode;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid SessionId { get; init; }
    public Guid PatientId { get; init; }
    public DateTime AbortedAtUtc { get; init; }
    public string ReasonCode { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid SessionId, out Guid PatientId, out DateTime AbortedAtUtc, out string ReasonCode)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        SessionId = this.SessionId;
        PatientId = this.PatientId;
        AbortedAtUtc = this.AbortedAtUtc;
        ReasonCode = this.ReasonCode;
    }
}

public sealed record IntradialyticAdverseEventIntegrationEvent : IIntegrationEvent
{
    public IntradialyticAdverseEventIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid SessionId,
        Guid PatientId,
        DateTime ObservedAtUtc,
        string EventKindCode,
        string Severity,
        string? Notes)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.SessionId = SessionId;
        this.PatientId = PatientId;
        this.ObservedAtUtc = ObservedAtUtc;
        this.EventKindCode = EventKindCode;
        this.Severity = Severity;
        this.Notes = Notes;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid SessionId { get; init; }
    public Guid PatientId { get; init; }
    public DateTime ObservedAtUtc { get; init; }
    public string EventKindCode { get; init; }
    public string Severity { get; init; }
    public string? Notes { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid SessionId, out Guid PatientId, out DateTime ObservedAtUtc, out string EventKindCode, out string Severity, out string? Notes)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        SessionId = this.SessionId;
        PatientId = this.PatientId;
        ObservedAtUtc = this.ObservedAtUtc;
        EventKindCode = this.EventKindCode;
        Severity = this.Severity;
        Notes = this.Notes;
    }
}
